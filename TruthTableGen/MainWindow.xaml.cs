using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TruthTableGen
{
    public class Evaluator
    {
        public class Field
        {
            public string leftOpd, rightOpd;
            public List<bool> fieldResult = new List<bool>();
            public char fieldOpr;
        }

        Dictionary<string, Field> evalPlan;
        string query;

        static char[] prec = { '(', '~', '&', '|', '>', '-', ')' };
        int findPrec(char inp)
        {
            for (int i = 0; i < prec.Length; i++) { if (prec[i] == inp) { return i; } }
            return -1;
        }

        /// <summary>
        /// This function is used to find the evaluation plan for the query
        ///     - It avoids multiple recalculations
        ///     - It provides a step by step method for evaluating the query
        ///     - It is a modified form of the original text-book evaluator
        ///     - My algorithm gets the plan without completely converting the expression to postfix
        /// </summary>
        /// <param name="query">The query to be evaluated</param>
        /// <returns>A dictionary with the evaluation plan</returns>
        Dictionary<string, Field> findEvalPlan()
        {
            //A pair of paranthesis is considered as the enclosing symbols for symbol balancing
            query = "(" + query + ")";

            //Required for getting the plan without having to convert to postfix
            Stack<char> boolOpr = new Stack<char>();
            Stack<string> boolOpd = new Stack<string>();

            //Stores the evaluation plan as a <key, value> pair for easy access
            Dictionary<string, Field> evalPlan = new Dictionary<string, Field>();

            //Check every element in the query
            foreach (var i in query)
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
                        if (boolOpd.Count != 0 && evalPlan.Keys.Contains(boolOpd.Peek()) == false) { evalPlan.Add(boolOpd.Peek(), columnField); }
                    }
                    else
                    {
                        //If the character is a valid symbol at a valid position
                        if (i != '(' && (boolOpr.Peek() != '(' || findPrec(boolOpr.Peek()) <= findPrec(i)))
                        {
                            //when the precedance becomes less then do evaluation plan generation
                            while (boolOpr.Peek() != '(' && findPrec(boolOpr.Peek()) <= findPrec(i))
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
                                if (boolOpd.Count != 0 && evalPlan.Keys.Contains(boolOpd.Peek()) == false) { evalPlan.Add(boolOpd.Peek(), columnField); }
                            }

                            //If the character is ')' remove its corresponding '(' to balance the equation
                            if (i == ')') { boolOpr.Pop(); } else { boolOpr.Push(i); }
                        }
                        else { boolOpr.Push(i); }   //Operators with higher precedance are simpley pushed into the operator stack
                    }
                }
            }

            return evalPlan;
        }

        /// <summary>
        ///     Used to return the result of evaluation of a boolean operation on an array of operands
        /// </summary>
        /// <param name="leftOpd">LHS of the boolean operator</param>
        /// <param name="rightOpd">RHS of the boolean operator</param>
        /// <param name="boolOpr">The boolean operator whose operation is to be simulated</param>
        /// <returns>A boolean array with the results of the evaluation</returns>
        List<bool> execOp(List<bool> leftOpd, List<bool> rightOpd, char boolOpr)
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
        int addVar(string keyVar, int varCount)
        {
            //Duplicate the redundant data
            foreach (var i in evalPlan)
            {
                i.Value.fieldResult.AddRange(i.Value.fieldResult);
            }

            //Add the new variable 
            //Assign TRUE to one pert and FALSE to the other equivalent part
            for (int i = 0; i < Math.Pow(2, varCount); i++)
            {
                evalPlan[keyVar].fieldResult.Add(true);
            }
            for (int i = 0; i < Math.Pow(2, varCount); i++)
            {
                evalPlan[keyVar].fieldResult.Add(false);
            }

            return ++varCount;
        }

        /// <summary>
        /// Evaluates the query and returns a DataView formatted Results table
        /// </summary>
        /// <param name="query">The query which needs to be evaluated</param>
        /// <returns>A DataView element with DataGrid Compatiblity</returns>
        public DataView evaluateQuery(string query)
        {
            //Set the query and evaluate it
            this.query = query;
            evalPlan = findEvalPlan();

            //If a new variable is to be added use addVar()
            //Otherwise, execute the operation
            int varCount = 0;
            foreach (var field in evalPlan)
            {
                if (field.Key.Length == 1) { varCount = addVar(field.Key, varCount); }  //For unary operators, the LHS is left free as NULL
                else { field.Value.fieldResult = execOp(field.Value.fieldOpr != '~' ? evalPlan[field.Value.leftOpd].fieldResult : null, evalPlan[field.Value.rightOpd].fieldResult, field.Value.fieldOpr); }
            }

            //Generate the table based on the results of the evaluation 
            //Return them to the calling function as a DataView
            return generateTable();
        }

        /// <summary>
        /// Uses the query to generate an evaluation plan for manual operations y humans
        /// </summary>
        /// <param name="query">The query which needs to be evaluated</param>
        /// <returns>A string which gives us the evaluation plan</returns>
        public string getEvaluationPlan(string query)
        {
            //Set the query and evaluate it
            this.query = query;
            evalPlan = findEvalPlan();

            string actualPlan = "";

            //The keys of the eval plan have the data needed
            foreach(var i in evalPlan.Keys)
            {
                actualPlan += "\n\n ** " + i;
            }

            return actualPlan;
        }

        /// <summary>
        /// Generates the table and also sorts them
        /// </summary>
        /// <returns>A DataView with the sorted TruthTable as a result</returns>
        DataView generateTable()
        {
            DataTable truthTable = new DataTable();

            //Create empty columns as place holders for the table
            //Use the key as the Row heading
            foreach (var column in evalPlan)
            {
                truthTable.Columns.Add(column.Key + "\b");
            }

            //foreach row in the results column add each column to the truthTable
            //Map true: T and false: F
            for (int i = 0; i < evalPlan.ElementAt(0).Value.fieldResult.Count; i++)
            {
                DataRow tableRow = truthTable.NewRow();

                for (int j = 0; j < evalPlan.Count; j++)
                {
                    tableRow[j] = evalPlan.ElementAt(j).Value.fieldResult[i] ? 'T' : 'F';
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes the mainwindow components
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
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
                //Select an operation based on the button clicked
                //Get the query from the text box and route it to the appropriate area
                switch ((sender as Button).Name)
                {
                    case "Go":
                        TruthTable.ItemsSource = new Evaluator().evaluateQuery(Query.Text);
                        break;
                    case "GetPlan":
                        new Solution(new Evaluator().getEvaluationPlan(Query.Text)).Show();
                        break;
                    case "Instructions":
                        string Instr = "Instructions : " + "\n" +
                            "The definition of the symbols are as follows" + "\n\t" +
                            "1. & = AND operator" + "\n\t" +
                            "2. | = OR operator" + "\n\t" +
                            "3. ~ = NOT operator" + "\n\t" +
                            "4. > = IMPLIES operator" + "\n\t" +
                            "5. - = BI-CONDITIONAL operator" + "\n" +
                            "Get Plan: Gives you a copy of the plan used to evaluate the expression" + "\n" +
                            "Go: Gives the result of the evaluation using the generated evalutation plan";
                        MessageBox.Show(Instr);
                        break;
                }
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
    }
}
