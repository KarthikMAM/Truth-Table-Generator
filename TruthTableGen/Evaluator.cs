using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string Query { get; set; }

        /// <summary>
        /// Finds the precedance
        /// </summary>
        /// <param name="inp">The operator of type int</param>
        /// <returns>The precedance of the operator (Lower the value higher the precedance)</returns>
        public static char[] prec = { '(', '¬', '∧', '∨', '→', '↔', ')' };
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
                                if (boolOpr.Peek() == '¬')
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
        /// This is used to get the final result column
        /// </summary>
        /// <returns>The column with the truth values</returns>
        public bool[] GetResultData()
        {
            //The result field is the column with the biggest key value
            string result = "";
            bool[] resultData = null;
            foreach (var i in EvalPlan) { if (result.Length < i.Key.Length) { result = i.Key; resultData = i.Value.fieldResult.ToArray(); } }
            return resultData;
        }

        /// <summary>
        /// Used to return the result of evaluation of a boolean operation on an array of operands
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
                case '¬':
                    for (int i = 0; i < rightOpd.Count; i++) { resultOpd.Add(!rightOpd[i]); }
                    break;
                case '∧':
                    for (int i = 0; i < leftOpd.Count; i++) { resultOpd.Add(leftOpd[i] && rightOpd[i]); }
                    break;
                case '∨':
                    for (int i = 0; i < leftOpd.Count; i++) { resultOpd.Add(leftOpd[i] || rightOpd[i]); }
                    break;
                case '→':
                    for (int i = 0; i < leftOpd.Count; i++) { resultOpd.Add(!leftOpd[i] || rightOpd[i]); }
                    break;
                case '↔':
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
                else { field.Value.fieldResult = ExecOp(field.Value.fieldOpr != '¬' ? EvalPlan[field.Value.leftOpd].fieldResult : null, EvalPlan[field.Value.rightOpd].fieldResult, field.Value.fieldOpr); }
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
                if (x.ColumnName.Length == 2) { tableViewSort += x.ColumnName + " DESC , "; }
            }
            tableView.Sort = tableViewSort.Remove(tableViewSort.Length - 3, 3);

            return tableView;
        }

        /// <summary>
        /// Find the pcnf of the query
        /// </summary>
        /// <returns>The string that represents the PCNF</returns>
        public string FindPCNF() { return FindNormalForm(false, '∨', '∧'); }

        /// <summary>
        /// Find the pdnf of the query
        /// </summary>
        /// <returns>The string that represents the PDNF</returns>
        public string FindPDNF() { return FindNormalForm(true, '∧', '∨'); }

        /// <summary>
        /// This function is used to find the PCNF and PDNF of the given query
        /// The procedure is based on the truth table
        /// </summary>
        /// <param name="EvalPlan">The results of the query after evaluation</param>
        /// <param name="truth">Whether it is a minterm or maxterm</param>
        /// <param name="op1">The operator within brackets</param>
        /// <param name="op2">The operator outside the brackets</param>
        /// <returns>The string with the normal form which is either a pcnf or pdnf</returns>
        private string FindNormalForm(bool truth, char op1, char op2)
        {
            //Initialize the normal form string
            string normalForm = "";

            //Get the result field and the variables field
            string resultId = "";
            Field result = null;
            List<KeyValuePair<string, Field>> variables = new List<KeyValuePair<string, Field>>();
            foreach (var i in EvalPlan)
            {
                if (resultId.Length < i.Key.Length) { result = i.Value; resultId = i.Key; }
                if (i.Key.Length == 1) { variables.Add(i); }
            }

            //Produce the minterms and the maxterms
            for (int i = 0; i < result.fieldResult.Count; i++)
            {
                //If it equals the truth value
                //Then add the new term
                if (result.fieldResult[i] == truth)
                {
                    normalForm += " ( ";
                    foreach (var j in variables)
                    {
                        //If the current value is false
                        //Then add the negation operator
                        if (j.Value.fieldResult[i] == false) { normalForm += "¬"; } else { normalForm += "  "; }
                        normalForm += j.Key + " " + op1 + " ";
                    }
                    //Delete the extra operator
                    normalForm = normalForm.Substring(0, normalForm.Length - 3);
                    normalForm += " ) " + op2 + " \n";
                }
            }
            //Delete the extra operator
            normalForm = normalForm.Substring(0, normalForm.Length - 3);

            //return the normal form string
            return normalForm;
        }
    }
}
