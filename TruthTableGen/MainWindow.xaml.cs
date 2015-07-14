using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TruthTableGen
{
    /// <summary>
    /// The container for holding the data of each column
    /// </summary>
    public class Field
    {
        public string leftOpd, rightOpd;
        public List<bool> fieldResult = new List<bool>();
        public char fieldOpr;
    }

    /// <summary>
    /// Evaluation engine for the query
    /// </summary>
    public class Evaluator
    {
        public Dictionary<string, Field> EvalPlan { get; set; }
        public string Query;

        /// <summary>
        /// Finds the precedance
        /// </summary>
        /// <param name="inp">The operator of type int</param>
        /// <returns>The precedance of the operator (Lower the value higher the precedance)</returns>
        static char[] prec = { '(', '~', '&', '|', '>', '-', ')' };
        int FindPrec(char inp)
        {
            for (int i = 0; i < prec.Length; i++) { if (prec[i] == inp) { return i; } }
            return -1;
        }

        /// <summary>
        /// Constructor which initializes the Query
        /// </summary>
        /// <param name="Query">The Query which is to be run</param>
        public Evaluator(string Query) { this.Query = "(" + Query + ")"; }

        /// <summary>
        /// This function is used to find the evaluation plan for the Query
        ///     - It avoids multiple recalculations
        ///     - It provides a step by step method for evaluating the Query
        ///     - It is a modified form of the original text-book evaluator
        ///     - My algorithm gets the plan without completely converting the expression to postfix
        /// </summary>
        /// <returns>A dictionary with the evaluation plan</returns>
        public Dictionary<string, Field> FindEvalPlan()
        {
            //Required for getting the plan without having to convert to postfix
            Stack<char> boolOpr = new Stack<char>();
            Stack<string> boolOpd = new Stack<string>();

            //Stores the evaluation plan as a <key, value> pair for easy access
            EvalPlan = new Dictionary<string, Field>();

            //Check every element in the Query
            foreach (var i in Query)
            {
                if (i != ' ')
                {
                    //Checking between operator and operand
                    if (prec.Contains<char>(i) == false)
                    {
                        //Operands are pushed into the operand queue
                        boolOpd.Push(i.ToString());

                        //The first occurances are used for generating test-cases
                        //So, they form a part of the evaluation plan
                        Field columnField = new Field();
                        if (boolOpd.Count != 0 && EvalPlan.Keys.Contains(boolOpd.Peek()) == false) { EvalPlan.Add(boolOpd.Peek(), columnField); }
                    }
                    else
                    {
                        //If the character is a valid symbol at a valid position
                        if (i != '(' && (boolOpr.Peek() != '(' || FindPrec(boolOpr.Peek()) <= FindPrec(i)))
                        {
                            //when the precedance becomes less then do evaluation plan generation
                            while (boolOpr.Peek() != '(' && FindPrec(boolOpr.Peek()) <= FindPrec(i))
                            {
                                //Get the plan according to the operator
                                Field columnField = new Field();
                                if (boolOpr.Peek() == '~')
                                {
                                    columnField.rightOpd = boolOpd.Pop();
                                    columnField.fieldOpr = boolOpr.Pop();
                                    boolOpd.Push("(" + " " + columnField.fieldOpr + " " + columnField.rightOpd + " " + ")");
                                }
                                else
                                {
                                    columnField.rightOpd = boolOpd.Pop();
                                    columnField.fieldOpr = boolOpr.Pop();
                                    columnField.leftOpd = boolOpd.Pop();
                                    boolOpd.Push("(" + " " + columnField.leftOpd + " " + columnField.fieldOpr + " " + columnField.rightOpd + " " + ")");
                                }

                                //If the plan was not already generated then add it
                                //Otherwise, discard to avoid redundancy
                                if (boolOpd.Count != 0 && EvalPlan.Keys.Contains(boolOpd.Peek()) == false) { EvalPlan.Add(boolOpd.Peek(), columnField); }
                            }

                            //If the character is ')' remove its corresponding '(' to balance the equation
                            if (i == ')') { boolOpr.Pop(); } else { boolOpr.Push(i); }
                        }
                        else { boolOpr.Push(i); }   //Operators with higher precedance are simpley pushed into the operator stack
                    }
                }
            }

            return EvalPlan;
        }

        /// <summary>
        ///     Used to return the result of evaluation of a boolean operation on an array of operands
        /// </summary>
        /// <param name="leftOpd">LHS of the boolean operator</param>
        /// <param name="rightOpd">RHS of the boolean operator</param>
        /// <param name="boolOpr">The boolean operator whose operation is to be simulated</param>
        /// <returns>A boolean array with the results of the evaluation</returns>
        List<bool> ExecOp(List<bool> leftOpd, List<bool> rightOpd, char boolOpr)
        {
            List<bool> resultOpd = new List<bool>();

            //Evalutate the operation of the operator
            //Since, negation is a unary operator - use only the RHS operand 
            switch (boolOpr)
            {
                case '~':
                    for (int i = 0; i < rightOpd.Count; i++) { resultOpd.Add(!rightOpd[i]); }
                    break;
                case '&':
                    for (int i = 0; i < leftOpd.Count; i++) { resultOpd.Add(leftOpd[i] && rightOpd[i]); }
                    break;
                case '|':
                    for (int i = 0; i < leftOpd.Count; i++) { resultOpd.Add(leftOpd[i] || rightOpd[i]); }
                    break;
                case '>':
                    for (int i = 0; i < leftOpd.Count; i++) { resultOpd.Add(!leftOpd[i] || rightOpd[i]); }
                    break;
                case '-':
                    for (int i = 0; i < leftOpd.Count; i++) { resultOpd.Add(leftOpd[i] == rightOpd[i]); }
                    break;
            }

            return resultOpd;
        }

        /// <summary>
        /// This is used to duplicate data fields upon the introduction of new variables
        /// This is cost-efficient because redundant caluclations are avoided
        /// Since these redundant data are simply duplicated
        /// </summary>
        /// <param name="keyVar">The key that is going to be introduced</param>
        /// <param name="varCount">The current number of variables already existing</param>
        /// <returns>The incremented number of variables</returns>
        int AddVar(string keyVar, int varCount)
        {
            //Duplicate the redundant data
            foreach (var i in EvalPlan)
            {
                i.Value.fieldResult.AddRange(i.Value.fieldResult);
            }

            //Add the new variable 
            //Assign TRUE to one pert and FALSE to the other equivalent part
            for (int i = 0; i < Math.Pow(2, varCount); i++)
            {
                EvalPlan[keyVar].fieldResult.Add(true);
            }
            for (int i = 0; i < Math.Pow(2, varCount); i++)
            {
                EvalPlan[keyVar].fieldResult.Add(false);
            }

            return ++varCount;
        }

        /// <summary>
        /// Evaluates the Query and returns a DataView formatted Results table
        /// </summary>
        /// <returns>A DataView element with DataGrid Compatiblity</returns>
        public DataView EvaluateQuery()
        {
            //Get the evaluation plan
            EvalPlan = FindEvalPlan();

            //If a new variable is to be added use addVar()
            //Otherwise, execute the operation
            int varCount = 0;
            foreach (var field in EvalPlan)
            {
                if (field.Key.Length == 1) { varCount = AddVar(field.Key, varCount); }  //For unary operators, the LHS is left free as NULL
                else { field.Value.fieldResult = ExecOp(field.Value.fieldOpr != '~' ? EvalPlan[field.Value.leftOpd].fieldResult : null, EvalPlan[field.Value.rightOpd].fieldResult, field.Value.fieldOpr); }
            }

            //Generate the table based on the results of the evaluation 
            //Return them to the calling function as a DataView
            return GenerateTable();
        }

        /// <summary>
        /// Uses the Query to generate an evaluation plan for manual operations y humans
        /// </summary>
        /// <returns>A string which gives us the evaluation plan</returns>
        public string GetEvaluationPlan()
        {
            //Get the evaluation plan
            EvalPlan = FindEvalPlan();

            string actualPlan = "";

            //The keys of the eval plan have the data needed
            foreach (var i in EvalPlan.Keys)
            {
                actualPlan += "\n\n ** " + i;
            }

            return actualPlan;
        }

        /// <summary>
        /// Generates the table and also sorts them
        /// </summary>
        /// <returns>A DataView with the sorted TruthTable as a result</returns>
        DataView GenerateTable()
        {
            DataTable truthTable = new DataTable();

            //Create empty columns as place holders for the table
            //Use the key as the Row heading
            foreach (var column in EvalPlan)
            {
                truthTable.Columns.Add(column.Key + "\b");
            }

            //foreach row in the results column add each column to the truthTable
            //Map true: T and false: F
            for (int i = 0; i < EvalPlan.ElementAt(0).Value.fieldResult.Count; i++)
            {
                DataRow tableRow = truthTable.NewRow();

                for (int j = 0; j < EvalPlan.Count; j++)
                {
                    tableRow[j] = EvalPlan.ElementAt(j).Value.fieldResult[i] ? 'T' : 'F';
                }

                truthTable.Rows.Add(tableRow);
            }

            //Create a default view and sort it based on the columns with the variables in ascending
            //Each column gets an iteration of F's and T's
            //The continuous count decreases by 2 for each successive variable
            DataView tableView = truthTable.DefaultView;
            string tableViewSort = "";
            foreach (DataColumn x in truthTable.Columns)
            {
                if (x.ColumnName.Length == 2) { tableViewSort += x.ColumnName + " ASC , "; }
            }
            tableView.Sort = tableViewSort.Remove(tableViewSort.Length - 3, 3);

            return tableView;
        }
    }

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
        public class Tree
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
            public Tree(Field data, string result, int yOffset, int xOffset)
            {
                this.data = data;
                this.result = result;
                this.yOffset = yOffset;
                this.xOffset = xOffset;

                InitializeGraphics();
            }

            /// <summary>
            /// This function initializes the graphics components for the Tree view
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
            public static Line Connector(double x1, double y1, double x2, double y2)
            {
                //Create a new line
                Line connector = new Line();

                //Set the start and the end points of the line
                connector.X1 = x1 + SIZE / 2;
                connector.Y1 = y1 + SIZE / 2;
                connector.X2 = x2 + SIZE / 2;
                connector.Y2 = y2 + SIZE / 2;

                //Add some house-keeping
                connector.Stroke = Brushes.Black;
                connector.HorizontalAlignment = HorizontalAlignment.Left;
                connector.VerticalAlignment = VerticalAlignment.Top;

                return connector;
            }
        }

        /// <summary>
        /// Construtor to initalize the window
        /// It also initializes the Tree view
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
            //Then using its results adjust the window height and width
            TreePlan.Children.Clear();
            Draw(finalResult, 1);
        }

        /// <summary>
        /// Creates the tree based on the evalPlan
        /// </summary>
        /// <param name="key">The current node's identifier on the evaluation plan dictionary</param>
        /// <param name="yOffset">The level at which the current node is supposed to be printed</param>
        /// <returns>The current node for further processing</returns>
        public Tree Draw(string key, int yOffset)
        {
            //Extract the current node from the Dictionary class instance
            Field currentField = evalPlan[key];
            Tree leftNode = null, rightNode = null;

            //Inorder Traversal: Traverses the left node then the central node and then the right node
            //The left and right nodes are needed for creating the connectors
            if (key.Length != 1 && currentField.fieldOpr != '~') { leftNode = Draw(currentField.leftOpd, yOffset + 1); }    //The negation operator will not have a left node
            Tree currentNode = new Tree(currentField, key, yOffset, xOffset++);
            if (key.Length != 1) { rightNode = Draw(currentField.rightOpd, yOffset + 1); }

            //If there is a left node then create the connector for the left node and the parent node
            //Here we use Insert(0, *) instead of .Add(*) because we want to display the Bubbles over he Connecting lines
            if (leftNode != null)
            {
                TreePlan.Children.Insert(0, Tree.Connector(currentNode.ellipse.Margin.Left,
                                                        currentNode.ellipse.Margin.Top,
                                                        leftNode.ellipse.Margin.Left,
                                                        leftNode.ellipse.Margin.Top));
            }

            //Add the contents of the current node
            TreePlan.Children.Add(currentNode.ellipse);
            TreePlan.Children.Add(currentNode.content);

            //If there is a right node then create the connector for the left node and the parent node
            //Here we use Insert(0, *) instead of .Add(*) because we want to display the Bubbles over he Connecting lines
            if (rightNode != null)
            {
                TreePlan.Children.Insert(0, Tree.Connector(currentNode.ellipse.Margin.Left,
                                                        currentNode.ellipse.Margin.Top,
                                                        rightNode.ellipse.Margin.Left,
                                                        rightNode.ellipse.Margin.Top));
            }

            //Return the current node
            return currentNode;
        }
    }



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes the main window components
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //Give a default Instruction
            string Instr = "Instructions : " + "\n\t*" +
                "The definition of the symbols are as follows" + "\n\t\t" +
                "1. & = AND operator" + "\n\t\t" +
                "2. | = OR operator" + "\n\t\t" +
                "3. ~ = NOT operator" + "\n\t\t" +
                "4. > = IMPLIES operator" + "\n\t\t" +
                "5. - = BI-CONDITIONAL operator" + "\n\t*" +
                "Get Plan: Gives you a copy of the plan used to evaluate the expression" + "\n\t*" +
                "Go: Gives the result of the evaluation using the generated evalutation plan";
            Instruction.Text = Instr;
        }

        /// <summary>
        /// Call back for the button click event
        /// </summary>
        /// <param name="sender">Represents the button clicked</param>
        /// <param name="e">Eventargs</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Create an instance of the evaluator class
                Evaluator evaluator = new Evaluator(Query.Text);

                //Display a heading on each tab
                EvalLabel.Content = TableLabel.Content = PlanLabel.Content = "Query: " + evaluator.Query;

                //Update the truth Table, tree view and the plan textbox
                TruthTable.ItemsSource = evaluator.EvaluateQuery();
                new TreeDiag(evaluator.EvalPlan, TreePlan);
                Plan.Text = evaluator.GetEvaluationPlan();
            }
            catch
            {
                //If, at all anything goes wrong
                //The only possible case is when the symbols are unbalanced
                //Or, there is no input in the text-box
                if (Query.Text.Length == 0) { MessageBox.Show("No Query in the Text Box", "No Query"); }
                else { MessageBox.Show("Warning: Unbalanced Symbols found in the Stack", "Error in Query"); }
            }
        }

        /// <summary>
        /// This will be called to update the form elements when the window's size is changed
        /// </summary>
        /// <param name="sender">The window whose size is changed</param>
        /// <param name="e">The eventargs that has the new size</param>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Correct the parameters of the TabContainer
            TabContainer.Width = e.NewSize.Width - 40;
            TabContainer.Height = e.NewSize.Height - 150;

            //Correct the parameters of the Go Button
            var goMargin = Go.Margin;
            goMargin.Left = TabContainer.Width + TabContainer.Margin.Left - Go.Width;
            Go.Margin = goMargin;

            //Correct the parameters of the Query TextBox
            var queryMargin = Query.Margin;
            queryMargin.Left = TabContainer.Margin.Left;
            Query.Width = goMargin.Left - 10 - queryMargin.Left;
            Query.Margin = queryMargin;
        }
    }
}
