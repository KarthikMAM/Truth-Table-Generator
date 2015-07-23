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
                "PCNF, PDNF: Gives the PCNF and PDNF of the query" + "\n\t*" +
                "Plan Tree: Gives the parsing tree of the query" + "\n\t*" +
                "Evaluations Plan: Gives you a copy of the plan used to evaluate the expression" + "\n\t*" +
                "Equivalence Test: Tells you whether two expressions are euivalent or not" + "\n\t*" +
                "Go: Gives the result of the evaluation using the generated evalutation plan";
            Instruction.Text = Instr;
        }

        /// <summary>
        /// Call back for the button click event
        /// </summary>
        /// <param name="sender">Represents the button clicked</param>
        /// <param name="e">Eventargs</param>
        private void Button_Click(object sender, EventArgs e)
        {
            try
            {
                //Format the input text to change the operators
                Query.Text = FormatInput(Query.Text);

                //Create an instance of the evaluator class
                Evaluator evaluator = new Evaluator(Query.Text);

                //Display a heading on each tab
                PdnfLabel.Content = PcnfLabel.Content = EvalLabel.Content = TableLabel.Content = PlanLabel.Content = "Query: " + evaluator.Query;

                //Update the truth Table, tree view and the plan textbox
                TruthTable.ItemsSource = evaluator.EvaluateQuery();
                new TreeDiag(evaluator.EvalPlan, TreePlan);
                Pcnf.Text = evaluator.FindPCNF();
                Pdnf.Text = evaluator.FindPDNF();
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

            //Correct the parameters of the Compare Button
            goMargin = Compare.Margin;
            goMargin.Left = TabContainer.Width + TabContainer.Margin.Left - Go.Width - 30;
            Compare.Margin = goMargin;

            //Correct the parameters of the CompareQuery TextBox
            queryMargin = CompareQuery.Margin;
            queryMargin.Left = TabContainer.Margin.Left;
            CompareQuery.Width = goMargin.Left - 10 - queryMargin.Left;
            CompareQuery.Margin = queryMargin;
        }

        /// <summary>
        /// Formats the input to replace the dummy operators with their actual symbol
        /// </summary>
        /// <param name="inputText">The text which contains the boolean expression</param>
        /// <returns>The formatted string with the correct operator syymbols</returns>
        private string FormatInput(string inputText)
        {
            //Replacing all the dummy operators with their actual symbols
            inputText = inputText.Replace('~', '¬');
            inputText = inputText.Replace('|', '∨');
            inputText = inputText.Replace('&', '∧');
            inputText = inputText.Replace('-', '↔');
            inputText = inputText.Replace('>', '→');

            for(int i=0; i<inputText.Length; i++)
            {
                if (Char.IsLetter(inputText[i]) == true  || Evaluator.prec.Contains(inputText[i]) == true || inputText[i] == ' ') { }
                else { inputText = inputText.Remove(i); }
            }
            return inputText;
        }

        /// <summary>
        /// Called when we want to compare two strings
        /// </summary>
        /// <param name="sender">Compare Button</param>
        /// <param name="e">Event Args</param>
        private void Compare_Click(object sender, EventArgs e)
        {
            try
            {
                //Format the input text to change the operators
                Query.Text = FormatInput(Query.Text);
                CompareQuery.Text = FormatInput(CompareQuery.Text);

                //Create an instance of the evaluator class
                Evaluator evaluator1 = new Evaluator(Query.Text);
                Evaluator evaluator2 = new Evaluator(CompareQuery.Text);

                //Evaluate query and find the results
                evaluator1.EvaluateQuery();
                evaluator2.EvaluateQuery();

                //Get the results array
                bool[] query1Result = evaluator1.GetResultData();
                bool[] query2Result = evaluator2.GetResultData();

                //Compare each of the result fields
                bool equalFlag = false;
                if(query1Result.Length == query2Result.Length)
                {
                    equalFlag = true;
                    for (int i = 0; i < query1Result.Length; i++) { if (query1Result[i] != query2Result[i]) { equalFlag = false; break; } }
                }

                //Display a message box according to the result
                if (equalFlag == true) { MessageBox.Show("The two expressions are Equivalent", "Equivalent Expressions"); }
                else { MessageBox.Show("The two expressions are not Equivalent", "Non - Equivalent Expressions"); }
                
            }
            catch
            {
                //If, at all anything goes wrong
                //The only possible case is when the symbols are unbalanced
                //Or, there is no input in the text-box
                if (Query.Text.Length == 0 || CompareQuery.Text.Length == 0) { MessageBox.Show("No Query in the Text Box", "No Query"); }
                else { MessageBox.Show("Warning: Unbalanced Symbols found in the Stack", "Error in Query"); }
            }
        }

        private void Query_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox sendText = sender as TextBox;
            sendText.Text = FormatInput(sendText.Text);
            sendText.Select(sendText.Text.Length, 0);
        }

        private void Query_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) 
            {
                switch((sender as TextBox).Name)
                {
                    case "Query":
                        Button_Click(sender, new EventArgs());
                        break;
                    case "Compare":
                        Compare_Click(sender, new EventArgs());
                        break;

                }
            }
        }
    }
}