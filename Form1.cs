namespace MyInterpreter;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
    }
    
    private void CompileButton_Click(object sender, EventArgs e)
    {
        try
        {
            var lex = new Lexer(textBox1.Text);
            Token t;
            while (true)
            {
                t=lex.NextToken();
                MessageBox.Show(t.value.ToString());
                if(t.type==TokenType.Eof)
                    break;
            }
            
            
            MessageBox.Show($"Скомпилировано успешно! Строк {textBox1.Text.Split('\n').Length}");
        }
        catch (ComplierExceptions.LexerException ex)
        {
            MessageBox.Show(ComplierExceptions.OutPutError(ex.Message,ex,textBox1.Text.Split('\n')));
        }
        
    }

    
}