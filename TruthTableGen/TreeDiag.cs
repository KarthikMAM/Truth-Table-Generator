using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TruthTableGen
{

    /// <summary>
    /// This class is used to create a virtual tree and its pictorial representation from the evaluation plan
    /// </summary>
    public class TreeDiag
    {
        //Class Scope variables
        Dictionary<string, Field> evalPlan;
        string finalResult;
        int xOffset;
        Grid TreePlan;

        /// <summary>
        /// This class contains the basic functionalities required to produce a tree Diagram of the evaluation plan
        /// </summary>
        public class TreeNode
        {
            //Static fields for reusability
            public static int OFFSET = 60;
            public static int SIZE = 40;

            //Data members
            Field data;
            string result;
            int yOffset, xOffset;
            public Ellipse ellipse;
            public Label content;

            /// <summary>
            /// This constructor initializes the class
            /// </summary>
            /// <param name="data">The data for the nodes from the evalPlan</param>
            /// <param name="result">This is a string that represents the result of the nodes operation</param>
            /// <param name="yOffset">The yOffset from the top that indicates the depth of the node</param>
            /// <param name="xOffset">The position of the node along the x - axis</param>
            public TreeNode(Field data, string result, int yOffset, int xOffset)
            {
                this.data = data;
                this.result = result;
                this.yOffset = yOffset;
                this.xOffset = xOffset;

                InitializeGraphics();
            }

            /// <summary>
            /// This function initializes the graphics components for the node of the Tree
            /// </summary>
            void InitializeGraphics()
            {
                //Create a new ellipse and a label to show the operation
                ellipse = new Ellipse();
                content = new Label();

                //Set the size and width of the ellipse to its default values
                content.Height = ellipse.Height = SIZE;
                content.Width = ellipse.Width = SIZE;

                //Make each node organised as a tree by using its yOffset and xOffset
                var margin = ellipse.Margin;
                margin.Top = yOffset * OFFSET;
                margin.Left = xOffset * OFFSET;
                margin.Right = 0;
                margin.Bottom = 0;
                content.Margin = ellipse.Margin = margin;

                //Select a color for the Ellipse
                ellipse.Stroke = Brushes.Black;
                ellipse.Fill = Brushes.LightGray;

                //Set the label parameters, so that the operation is displayed properly
                content.Content = result.Length != 1 ? data.fieldOpr.ToString() : result;
                content.HorizontalContentAlignment = HorizontalAlignment.Center;
                content.VerticalContentAlignment = VerticalAlignment.Center;
                content.ToolTip = result;
                content.FontSize = 15;
                content.FontWeight = FontWeights.ExtraBold;

                //Align the graphics elements to a left top margin system
                content.HorizontalAlignment = ellipse.HorizontalAlignment = HorizontalAlignment.Left;
                content.VerticalAlignment = ellipse.VerticalAlignment = VerticalAlignment.Top;
            }

            /// <summary>
            /// This function creates a connector line between two nodes of the tree
            /// </summary>
            /// <param name="x1">The starting point's X coordinate</param>
            /// <param name="y1">The starting point's Y coordinate</param>
            /// <param name="x2">The ending point's X coordinate</param>
            /// <param name="y2">The ending point's Y coordinate</param>
            /// <returns>The generated line between the nodes</returns>
            public static Line Connector(TreeNode node1, TreeNode node2)
            {
                //Create a new line
                Line connector = new Line();

                //Set the start and the end points of the line
                connector.X1 = node1.ellipse.Margin.Left + SIZE / 2;
                connector.Y1 = node1.ellipse.Margin.Top + SIZE / 2;
                connector.X2 = node2.ellipse.Margin.Left + SIZE / 2;
                connector.Y2 = node2.ellipse.Margin.Top + SIZE / 2;

                //Add some house-keeping
                connector.Stroke = Brushes.Black;
                connector.HorizontalAlignment = HorizontalAlignment.Left;
                connector.VerticalAlignment = VerticalAlignment.Top;

                return connector;
            }
        }

        /// <summary>
        /// Construtor to initalize the window
        /// It also initializes the Tree view using the TreeNode class
        /// </summary>
        /// <param name="evalPlan">The evaluation plan of the Query sent from the MainWindow class</param>
        /// <param name="TreePlan">The Grid where the Tree is to be inserted</param>
        public TreeDiag(Dictionary<string, Field> evalPlan, Grid TreePlan)
        {
            //Assign the variables
            this.TreePlan = TreePlan;
            this.evalPlan = evalPlan;
            this.finalResult = "";
            this.xOffset = 0;

            //Find the result which is the key with the largest length
            //And set it as the title of this window and the content of the Infolabel
            foreach (var i in evalPlan.Keys) { if (i.Length > finalResult.Length) { finalResult = i; } }

            //Clear the window
            //Draw the tree
            TreePlan.Children.Clear();
            DrawNode(finalResult, 1);
        }

        /// <summary>
        /// Creates the tree based on the evalPlan using the TreeNode objects
        /// </summary>
        /// <param name="key">The current node's identifier on the evaluation plan dictionary</param>
        /// <param name="yOffset">The level at which the current node is supposed to be printed</param>
        /// <returns>The current node for further processing</returns>
        public TreeNode DrawNode(string key, int yOffset)
        {
            //Extract the current node from the Dictionary class instance
            Field currentField = evalPlan[key];
            TreeNode leftNode = null, rightNode = null;

            //Inorder Traversal: Traverses the left node then the central node and then the right node
            //The left and right nodes are needed for creating the connectors
            //Recursive call to create the left sub-tree
            if (key.Length != 1 && currentField.fieldOpr != '¬') { leftNode = DrawNode(currentField.leftOpd, yOffset + 1); }    //The negation operator will not have a left node

            //Add the contents of the current node
            //This will become the parent of the left and the right sub-trees
            TreeNode currentNode = new TreeNode(currentField, key, yOffset, xOffset++);
            TreePlan.Children.Add(currentNode.ellipse);
            TreePlan.Children.Add(currentNode.content);

            //Recursive call to create the right sub-tree
            if (key.Length != 1) { rightNode = DrawNode(currentField.rightOpd, yOffset + 1); }

            //If there is a left node then create the connector for the left node and the parent node
            //If there is a right node then create the connector for the left node and the parent node
            //Here we use Insert(0, *) instead of .Add(*) because we want to display the Bubbles over he Connecting lines
            if (leftNode != null) { TreePlan.Children.Insert(0, TreeNode.Connector(currentNode, leftNode)); }
            if (rightNode != null) { TreePlan.Children.Insert(0, TreeNode.Connector(currentNode, rightNode)); }

            //Return the current node
            return currentNode;
        }
    }
}
