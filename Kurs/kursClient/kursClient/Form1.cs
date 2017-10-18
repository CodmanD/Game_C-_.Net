using kursClient.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kursClient
{


    public partial class Form1 : Form
    {
        Graphics G;
        GameField gf;
        int status = 0;


        String IpServer = System.Configuration.ConfigurationManager.AppSettings["Ip"];
        int PortServer = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["port"]);
        TcpClient clientMaster = new TcpClient();
        TcpClient clientSlave = new TcpClient();
        NetworkStream NSMaster = null;
        NetworkStream NSSlave = null;


        static public ImageList imageList;
        public TextBox tb = new TextBox();
        bool isRight = false;
        bool isMove = false;

        bool isFirst = false;
        public Mutex m = new Mutex();
       private string opponentName;
        private string name;
        private int goalsMy=0;
        private int goalsOpponent=0;
        string email;
        string pass;
        string gender;


        Font font = new Font("Microsoft San Serif", 14);
        Button newGame = new Button();
        Button lookStat = new Button();
        Button exit = new Button();
        Label goalsMyLabel = new Label();
        Label goalsOpponentLabel = new Label();
        Thread threadForPaint;

        bool isMenu = true;

        public Form1()
        {
            InitializeComponent();
            threadForPaint = new Thread(runForPaint);
            //this.tableLayoutPanel1.BackColor = Color.LightGray;
            this.newGame.Text = "Играть";
            this.newGame.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.newGame.Font = this.font;
            this.newGame.Click += Button_Click;

            this.lookStat.Text = "Мой кабинет";
            this.lookStat.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.lookStat.Font = font;
            this.lookStat.Click += Button_Click;

            this.exit.Text = "Уйти";
            this.exit.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.exit.Font = this.font;
            this.exit.Click += Button_Click;

            this.goalsMyLabel.Anchor= AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.goalsOpponentLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.goalsMyLabel.BackColor = Color.White;
            this.goalsOpponentLabel.BackColor = Color.White;
            this.goalsMyLabel.Font =this.font;
            this.goalsOpponentLabel.Font = this.font;
            this.goalsMyLabel.TextAlign = ContentAlignment.MiddleCenter;
            this.goalsOpponentLabel.TextAlign = ContentAlignment.MiddleCenter;

            imageList = this.imageList1;

            this.tb.SetBounds(0, 0, 0, 0);
            this.tb.Focus();
            this.tb.Leave += Tb_Leave;

            this.tb.KeyDown += TB_KeyDown;
            this.tb.KeyUp += TB_KeyUp;
            this.Controls.Add(this.tb);
            this.tb.Focus();

            // this.imageList1.ImageSize = new Size(128,128);

            gf = new GameField();//Мой ЭУ 
           // gf.Size = new Size(this.Size.Height - 50, this.Height - 50);

            gf.BackColor = Color.Bisque;
            gf.Location = new Point(this.Location.X + 10, this.Location.Y + 10);
            gf.Anchor = AnchorStyles.Left | AnchorStyles.Right| AnchorStyles.Top | AnchorStyles.Bottom;

            this.SizeChanged += Form1_SizeChanged;
            this.Load += Form1_Load;
            this.button1.Click += Button1_Click;
            this.button2.Click += Button2_Click;

        }
        //--------Конец конструктора Form1------------
        private void Button2_Click(object sender, EventArgs e)//Обработка кнопки РЕГИСТРАЦИЯ
        {
            //if ((Button)sender == this.button2)

            if(gf.status==0)
            {
                Form2 f = new Form2();
                if (f.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        String gender = (f.checkBox1.Checked) ? "male" : "female";
                        String msg = "REGISTRY|" + f.textBox1.Text + "|" + f.textBox2.Text
                                       + "|" + f.textBox3.Text + "|" + gender;
                        msg = this.connectToServerForClientMaster(msg);

                        String[] z = msg.Split(new char[] { '|' });
                        if (z[0] == "REGISTRYOK")
                        {
                            // MessageBox.Show("Пользователь зарегистрирован ", "Server message");
                            this.name = z[1];
                            this.pass = z[2];
                            this.email = z[3];
                            this.gender = z[4];
                            AddInFormMenu();
                        }
                        else if (z[0] == "REGISTRYERROR")

                        {
                            // MessageBox.Show(msg, "ERROR");
                            if (MessageBox.Show(msg + "\nРегистрация не прошла\n", "ERROR Registry", MessageBoxButtons.OKCancel) ==
                                DialogResult.Cancel)
                            {
                                this.Close();
                                return;
                            }
                            else
                                return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                else
                {
                    this.Close();
                }
            }
    }

        private void startGame()//Запуск поля игры
        {
            //MessageBox.Show("StartGame for :"+name);

            // this.Text = name+":"+"Push Space ";
           // this.tableLayoutPanel1.BackColor = Color.White;
            this.goalsMy = 0;
            this.goalsOpponent = 0;
            this.Controls.Add(this.tb);
            this.tb.Focus();
            this.Width = 700;
            this.Height = 500;
            this.tableLayoutPanel1.Controls.Clear();
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Clear();

            this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));


            //this.tableLayoutPanel1.ColumnStyles[0].SizeType=SizeType.Percent;
            //this.tableLayoutPanel1.ColumnStyles[0].Width = 10;
            //this.tableLayoutPanel1.ColumnStyles[1].SizeType=SizeType.Percent;
        
            //this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));

         
            this.tableLayoutPanel1.Controls.Add(goalsMyLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(gf, 1, 0);
            this.tableLayoutPanel1.Controls.Add(goalsOpponentLabel, 2, 0);
            
            this.goalsMyLabel.Text =this.name+"\n"+ goalsMy.ToString();
            this.goalsOpponentLabel.Text =this.opponentName+"\n"+ this.goalsOpponent.ToString();

        }
        private void MyRoom()//Мой кабинет
        {
            this.Visible = false;
            Form3 f = new Form3();

            String msg = "BestPlayers|";
           // msg = connectToServerForClientMaster(msg);
           
            String[] z = msg.Split('|');
            //DialogResult dr = 
            //for (int i = 1; i < z.Length; i++)
            //    f.listView1.Items.Add(new ListViewItem(z[i]));
            while (true) {
                switch (f.ShowDialog())
                {
                    case DialogResult.OK://Изменить профиль
                        Form2 f2 = new Form2();
                        f2.textBox1.Text = this.name;
                        f2.textBox2.Text = this.pass;
                        f2.textBox3.Text = this.email;
                        f2.checkBox1.Checked=(this.gender=="male")?true:false;
                        f2.checkBox2.Checked = (f2.checkBox1.Checked) ? false : true;
                        if (f2.ShowDialog()== DialogResult.OK)
                        {
                            String gender = (f2.checkBox1.Checked) ? "male" : "female";
                            msg = "Update|" + f2.textBox1.Text + "|" + f2.textBox2.Text
                                           + "|" + f2.textBox3.Text + "|" + gender;
                            msg = this.connectToServerForClientMaster(msg);

                             z = msg.Split(new char[] { '|' });
                            if (z[0] == "UpdateOK")
                            {
                                MessageBox.Show("Профиль изменен");
                                this.name= f2.textBox1.Text  ;
                                f2.textBox2.Text = this.pass;
                                f2.textBox3.Text = this.email;
                                this.gender=(f2.checkBox1.Checked) ? "male" : "female";
                            }
                        }
                        break;

                    case DialogResult.Yes://Результаты игр
                        if (f.button3.Text == "Результаты игр")
                        {
                           // MessageBox.Show("За результатами");
                            msg = "Resultats|";
                            msg = connectToServerForClientMaster(msg);
                            z = msg.Split('|');
                            f.listView1.Items.Clear();
                            for (int i = 1; i < z.Length; i++)
                                f.listView1.Items.Add(new ListViewItem(z[i]));
                        }
                        if (f.button3.Text == "Лучшие игроки")
                        {
                            msg = "Resultats|";
                            msg = connectToServerForClientMaster(msg);
                            z = msg.Split('|');
                            for (int i = 1; i < z.Length; i++)
                                f.listView1.Items.Add(new ListViewItem(z[i]));
                        }
                        break;
                    case DialogResult.Cancel:
                        f.Close();
                        this.Visible = true;
                        //this.Invalidate();
                        return;
                        break;
                }
            }
            
        }
        private void Button_Click(object sender, EventArgs e)//ОСНОВНОЕ МЕНЮ ИГРЫ
        {
            switch (((Button)sender).Text)
            {
                case ("Играть"):
                    // this.Controls.Clear();
                    // MessageBox.Show(gf.status.ToString());
                    String msg = "StartGame|";
                     msg = connectToServerForClientMaster(msg);
                    String[] z = msg.Split(new char[] { '|' });
                    if (z[0] == "StartGameOK")
                    {
                       // this.Text = this.Text + "  for " + this.textBox1.Text;
                     //   MessageBox.Show("Старт игры");
                        if (z[1] == "One")  
                            this.gf.isFirst = true;
                        if (z[1] == "Two")
                        {
                            this.gf.isFirst = false;
                          //  MessageBox.Show(msg, "Second player:Player1="+z[5]);
                            this.opponentName = z[5];
                        }

                        if (!threadForPaint.IsAlive)
                        {

                            threadForPaint.IsBackground = true;
                            threadForPaint.Start();
                        }
                        // MessageBox.Show(msg, "ERROR");
                        this.gf.rows = Convert.ToInt32(z[2]);
                        this.gf.cols = Convert.ToInt32(z[3]);
                        this.gf.status = Convert.ToInt32(z[4]);
                        //gf.gameField = new int[gf.rows, gf.cols];
                       

                        double k = Convert.ToDouble(gf.rows) / Convert.ToDouble(gf.cols);
                        // MessageBox.Show("Rows= " + gf.rows + "\tCols= " + gf.cols);
                        // MessageBox.Show("Коэффициент= " + k + "OldWidth= " + this.Width + "New Width= " + this.Width * k + "\nOldHeigth" + this.Height + "NewHeigth" + this.Height * k);
                        if (gf.rows >= gf.cols)
                        {
                            this.Width = Convert.ToInt32(this.Width / k);
                            //this.Height = Convert.ToInt32(this.Height * k);
                        }
                        else
                        {
                            //this.Width = Convert.ToInt32(this.Width * k);
                            this.Height = Convert.ToInt32(this.Height / k);
                        }

                        gf.Width = this.Width - 35;
                        gf.Height = this.Height - 60;

                        if (this.gf.status == 2)
                        {
                            this.isMenu = false;
                            startGame();
                        }
                        else
                        {
                            this.newGame.Text = "Ожидайте подключения соперника";
                        }
                    }
                    break;
                case "Мой кабинет":
                   // MessageBox.Show("MyRoom");
                    
                   MyRoom();
                    break;
                case "Уйти":
                    this.Close();
                    break;
            }
        }

        private void Button1_Click(object sender, EventArgs e)//Обработка входа LOGIN
        {
            try
            {
                if (this.textBox1.Text == "")
                    this.errorProvider1.SetError(this.textBox1, "Введите имя");
                else
                    this.errorProvider1.Clear();
                if (this.textBox2.Text == "")
                    this.errorProvider1.SetError(this.textBox2, "Введите пароль");
                else
                    this.errorProvider1.Clear();
                String msg;
                if (this.textBox1.Text != ""&& this.textBox2.Text != "")
                {
                    this.name = this.textBox1.Text;
                    msg = "LOGIN|" + this.textBox1.Text + "|" + this.textBox2.Text;
                    msg = connectToServerForClientMaster(msg);
                    
                    String[] z = msg.Split(new char[] { '|' });
                    if (z[0] == "LOGINOK")
                    {
                        this.Text = this.Text + "for" + this.textBox1.Text;
                        //MessageBox.Show("Успешный вход");
                        this.pass = z[1];
                        this.email = z[2];
                        this.gender = z[3];
                        //gf.gameField = new int[gf.rows, gf.cols];

                        AddInFormMenu();
                    }
                    else if (z[0] == "LOGINERROR")
                    // MessageBox.Show(msg, "ERROR");
                    {
                        // MessageBox.Show(msg, "ERROR");
                        if (MessageBox.Show("\nПроверьте правильность ввода или зарегистрируйтесь\n", "Login Error", MessageBoxButtons.OKCancel) ==
                            DialogResult.Cancel)
                        {
                            this.Close();
                            return;
                        }
                        else
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LLOOGGIINN" + ex.Message);

            }
           
        }
        //-------------Добавляем на форму Меню
        private void AddInFormMenu()
        {
           // this.tableLayoutPanel1.BackColor = Color.LightGray;
            this.Text = "XX век.Наследие";
            this.tableLayoutPanel1.Width = this.Width-16;
            this.tableLayoutPanel1.Height = this.Height-40;


            this.tableLayoutPanel1.Controls.Clear();
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Clear();
            this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            this.tableLayoutPanel1.ColumnStyles[0].Width = this.Width;
            //this.tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Percent;
            //this.tableLayoutPanel1.ColumnStyles[0].Width =this.Width;





            //MessageBox.Show("Menu Created"+ this.tableLayoutPanel1.RowCount);
            //tableLayoutPanel1.RowStyles.Clear();

            // for (int i = 0; i < this.tableLayoutPanel1.RowCount; i++)
            //{
          //    tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent,33));

            //    // tableLayoutPanel1.RowStyles[i].Height = 30;// 100 / this.tableLayoutPanel1.RowCount;
            //}
            // MessageBox.Show("MENU"+tableLayoutPanel1.RowStyles.Count.ToString());
            this.tableLayoutPanel1.Controls.Add(newGame,0,0);
            this.tableLayoutPanel1.Controls.Add(lookStat,0,1);
            this.tableLayoutPanel1.Controls.Add(exit,0,2);

            //this.tableLayoutPanel1.SetRow(this.newGame, 0);
            //this.tableLayoutPanel1.SetRow(this.lookStat, 1);
            //this.tableLayoutPanel1.SetRow(this.exit, 2);
        }

        // ---------------ПРИ открытии Связываемся с сервером
        private void Form1_Load(object sender, EventArgs e)
        {

            try
            {
                clientMaster.Connect(IpServer, PortServer);
                clientSlave.Connect(IpServer, PortServer + 1);

                NSMaster = clientMaster.GetStream();
                NSSlave = clientSlave.GetStream();
                this.Width = 400;
                this.Height = 200;
                // this.listBox1.Items.Add("Соединение с сервером успешно");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }

        //-----------методы Управление игрой
        public void TB_KeyDown(object sender, KeyEventArgs e)//Управление игрой
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    //if(this.isFirst)
                    this.connectToServerForClientMaster("Move|left");
                    //else
                    //this.connectToServerForClientMaster("Move|right");
                    break;
                case Keys.Right:
                    //if (this.isFirst)
                    this.connectToServerForClientMaster("Move|right");
                    //else
                    //  this.connectToServerForClientMaster("Move|left");
                    break;
                case Keys.Space:
                case Keys.Enter:
                     // MessageBox.Show("ENTER");

                    if (gf.status == 2 || gf.status == 3)
                        this.connectToServerForClientMaster("Game|start");
                    break;
                case Keys.P:
                    // MessageBox.Show("PauseGame");
                    if (gf.status == 2 || gf.status == 3)
                        this.connectToServerForClientMaster("Game|pause");
                    break;
                case Keys.Escape:
                    //MessageBox.Show("Exit");
                    if (gf.status == 2 || gf.status == 3)
                        this.connectToServerForClientMaster("Game|stopGame");
                    //остановить игру
                    this.Controls.Clear();
                    this.newGame.Text = "Играть";
                    this.isMenu = true;
                    this.Width = 400;
                    this.Height = 200;

                    AddInFormMenu();
                    this.Controls.Add(this.tableLayoutPanel1);
                    break;
            }
        }

        public void TB_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    this.connectToServerForClientMaster("Move|stop");
                    break;
                case Keys.Right:
                    this.connectToServerForClientMaster("Move|stop");
                    break;
            }
        }

        //---------метод вторичного потока для текущего положения игрового поля--
        private void runForPaint()
        {
            // MessageBox.Show("From Server for gf " );
            int k = 0;
            String s = "";
            while (true)
            {
                try
                {
                    byte[] buf = new byte[16];

                    //Привет от сервера
                    while (true)//Пока есть данные для чтения
                    {
                        int cnt = NSSlave.Read(buf, 0, buf.Length);
                        if (cnt == 0) throw new Exception("Получено 0 байт");
                        // MS.Write(buf, 0, buf.Length);
                        if (!NSMaster.DataAvailable) break;
                    }
                    if (Convert.ToInt16(buf[0]) != 1)
                    {
                        int length = buf[1];
                        byte[] tmpBytes = new byte[buf.Length - 2];
                        for (int i = 2,j=0; i < buf.Length; i++)
                        {
                            tmpBytes[j++] = buf[i];
                        }
                        this.opponentName = Encoding.UTF8.GetString(tmpBytes).Remove(length);
                       // MessageBox.Show(gf.status.ToString());
                        if ( isMenu)
                        {
                            this.Invoke(new Action(startGame));
                            //MessageBox.Show("Start game Goals oppo - " + this.goalsOpponent);
                        }

                    }
                    else
                    {

                        if (gf.isFirst)
                        {
                            // MessageBox.Show(Convert.ToString(buf[1])+"BYTE # "+ Convert.ToInt16(buf[0]));
                            this.gf.status = Convert.ToInt16(buf[1]);
                            this.gf.positionBallX = Convert.ToInt16(buf[2]);
                            this.gf.positionBallY = Convert.ToInt16(buf[3]);
                            this.gf.currentPositionPlayer = Convert.ToInt16(buf[4]);
                            this.gf.currentPositionRival = Convert.ToInt16(buf[5]);
                            this.goalsMy = Convert.ToInt16(buf[6]);
                            this.goalsMyLabel.Text = this.name + "\n" + this.goalsMy.ToString();
                            this.goalsOpponent = Convert.ToInt16(buf[7]);
                            this.goalsOpponentLabel.Text = this.opponentName + "\n" + this.goalsOpponent.ToString();
                            // if(goalsOpponent==1)
                            // MessageBox.Show("Goals oppo - " + this.goalsOpponent);//+ "\nName oppo" + this.opponentName);
                            
                        }
                        else
                        {
                            this.gf.status = Convert.ToInt16(buf[1]);
                            this.gf.positionBallX = gf.cols - 3 - Convert.ToInt16(buf[2]);
                            this.gf.positionBallY = gf.rows - Convert.ToInt16(buf[3]);
                            this.gf.currentPositionPlayer = gf.cols - Convert.ToInt16(buf[5]);
                            this.gf.currentPositionRival = gf.cols - Convert.ToInt16(buf[4]);
                            this.goalsMy = Convert.ToInt16(buf[7]);
                            this.goalsMyLabel.Text = this.name + "\n" + this.goalsMy.ToString();
                            this.goalsOpponent = Convert.ToInt16(buf[6]);
                            this.goalsOpponentLabel.Text = this.opponentName + "\n"+this.goalsOpponent.ToString();
                        }

//MessageBox.Show(s);
                        gf.Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ClientSlave" + ex.Message);
                    this.Invoke(new Action(Close));
                    //return "";
                } 
                Thread.Sleep(30);
            }
        }


        public String connectToServerForClientMaster(String str)//связь с сервером как мастер
        {
            try
            {
                byte[] buf = new byte[16];
                MemoryStream MS = new MemoryStream();
                //String msg = "LOGIN|" + nick;
                //MessageBox.Show("To Server " + str);
                //запрос к серверу
                byte[] a = Encoding.UTF8.GetBytes(str);
                NSMaster.Write(a, 0, a.Length);

                //Ответ от сервера
                while (true)//Пока есть данные для чтения
                {
                    int cnt = NSMaster.Read(buf, 0, buf.Length);
                    if (cnt == 0) throw new Exception("Получено 0 байт");
                    MS.Write(buf, 0, cnt);
                    if (!NSMaster.DataAvailable) break;
                }
                //ОБработка ответа
                a = MS.ToArray();

                str = Encoding.UTF8.GetString(a, 0, a.Length);
                return str;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ClientMaster" + ex.Message);
                return "";
            }

        }

        private void Tb_Leave(object sender, EventArgs e)//При потере фокуса скрытого TextBox даем ему фокус
        {
            // MessageBox.Show("Потеря фокуса" + ((TextBox)sender).Name);
            ((TextBox)sender).Focus();
        }

        //Изменение размеров окна
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            m.WaitOne();
            try
            {
                if (!isMenu)
                {
                    //double k = Convert.ToDouble(gf.rows) / Convert.ToDouble(gf.cols);
                   double kX = Convert.ToDouble(this.Width) / Convert.ToDouble(gf.cols);
                   double kY = Convert.ToDouble(this.Height) / Convert.ToDouble(gf.rows);

                    //double kX = Convert.ToDouble(this.tableLayoutPanel1.Width) / Convert.ToDouble(this);

                    gf.Width = (kX >= kY) ? Convert.ToInt32(kY *gf.cols) : Convert.ToInt32(kX *gf.cols);
                    gf.Height = (kX >= kY) ? Convert.ToInt32(kY * gf.rows) : Convert.ToInt32(kX * gf.rows);

                    if (this.gf.Height > this.gf.Width)
                        gf.Width = gf.Height;
                    
                   // this.Width = gf.Width + 100;
                   // this.Height= gf.Height + 100;
                   // this.tableLayoutPanel1.ColumnStyles[1]. = gf.Height;

                    // this.Width = Convert.ToInt32(this.Height / k);
                    // gf.Width = this.Width - 35;
                    // gf.Height = this.Height - 60;
                }
            }
            catch (Exception ex)
            { }
            m.ReleaseMutex();
        }



    }
 
    
    //--------------------------Конец Form1--------------------------------

    public class GameField : Control
    {
        // public int[,] arr = new int[100,100];//1-граница 2-игроки 3-шарик
        bool isBall = true;
        //   public bool ballDown = true;
        //   public String s = "";

        public int status = 0;
        public bool isFirst = false;
        public int positionBallX;
        public int positionBallY;
        public int currentPositionPlayer = 0;
        public int startPositionPlayer;
        public int currentPositionRival = 0;
        public int startPositionRival;
        public int widthPlatform;

        Font font = new Font("Microsoft San Serif", 28);
        public int[,] gameField;
        public int rows;
        public int cols;
        private Image imageBack;
        private Image imageField;
        public GameField()
        {
            startPositionPlayer = rows - 10;
            startPositionRival = 10;
            this.imageBack= (Bitmap)Properties.Resources.Field;
            this.imageField = (Bitmap)Properties.Resources.gameFieldLast;
            //image.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;

            this.Paint += GameField_Paint;
            this.SizeChanged += GameField_SizeChanged;//event изменение размера
            this.DoubleBuffered = true;


        }


        private void GameField_SizeChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void GameField_Paint(object sender, PaintEventArgs e)
        {
            // MessageBox.Show("X=" + this.Location.X.ToString() + "\tY=" + this.Location.Y.ToString()
            //    +"\nLeft = "+this.Left+"\nRight = "+this.Right+"\nTop = "+this.Top+"\nBottom = "+this.Bottom
            //  +"\n Wigth"+this.Width+"\nHeigth"+this.Height);
            //MessageBox.Show("Width" + this.Width + "\tHeight" + this.Height);

            widthPlatform = cols / 5;
            startPositionPlayer = rows - 10;
            startPositionRival = 10;


            BufferedGraphicsContext context = BufferedGraphicsManager.Current;
            BufferedGraphics bGraph = context.Allocate(e.Graphics, new Rectangle(0, 0, this.Width, this.Height));
            Graphics g = bGraph.Graphics;

            float x = 0;
            float y = 0;

            //if (e.ClipRectangle.Width >= e.ClipRectangle.Height)
            //{
            //    // this.Width = e.ClipRectangle.Width;
            //     this.Height = e.ClipRectangle.Width;
            //    //this.Height = this.Width;
            //}
            //else
            //{
            //   // this.Width = this.Height;
            //    this.Width = e.ClipRectangle.Height;
            //    //this.Height = e.ClipRectangle.Height;
            //}

            x = (float)this.Width / cols;
            y = (float)this.Height / rows;

            float size=(Width<=Height)?this.Width:this.Height;
            x = (float)size / cols;
            y = (float)size / rows;
            // isBall = true;
            g.Clear(Color.White);

            // g.DrawImage(image, 0, 0);
          //  g.DrawImage(imageBack, 0, 0,
            //                  this.Width, this.Height);
            g.DrawImage(imageField,(this.Width - size) / 2, 0,
                               size, size );

            //g.DrawLine(Pens.Black, new PointF((this.Width - size) / 2, 0),
                                           //     new PointF((this.Width - size) / 2, size));
            //g.DrawLine(Pens.Black, new PointF(size, 0), new PointF(size, size));
            //g.DrawLines(Pens.Black, new PointF[] {new PointF((this.Width - size) / 2, 0),
            //                                    new PointF((this.Width - size) / 2, size),
            //                                    new PointF(size, 0),new PointF(size, size)});
            //g.DrawPath()

            if (this.status == 5)
            {
                if (isFirst)
                {
                    g.DrawString("Вы победили", this.font, Brushes.Red, Width/2-50, Height / 2);
                }
                
                else
                    g.DrawString("Вы проиграли", this.font, Brushes.Red, Width / 2 - 50, Height / 2);
               
            }


            if (this.status == 6)
            {
                if (!isFirst)
                    g.DrawString("Вы победили", this.font, Brushes.Red, Width / 2 - 50, Height / 2);
                //  MessageBox.Show(name + ":You win:Status6");
                else
                    g.DrawString("Вы проиграли", this.font, Brushes.Red, Width / 2 - 50, Height / 2);
                // MessageBox.Show(name + ":You lose:Status6");
            }

            // g.FillRectangle(Brushes.White, (this.Width-size)/2, 0,
            // size, size/2);
            // g.FillRectangle(Brushes.Black, (this.Width - size) / 2, size / 2,
            //                 size , size / 2);


            // MessageBox.Show((x * currentPositionPlayer).ToString());


            if (isFirst)
                g.FillRectangle(Brushes.White, ((this.Width - size) / 2)+x * currentPositionPlayer, startPositionPlayer * y,
                                x * widthPlatform, y * 3);
            //g.FillEllipse(Brushes.Green, x * currentPositionPlayer, startPositionPlayer * y,
            //                  x * widthPlatform, y * 3);
            else
                g.FillRectangle(Brushes.White, ((this.Width - size) / 2) + x * (currentPositionPlayer - widthPlatform), startPositionPlayer * y,
                                x * widthPlatform, y * 3);
            //g.FillEllipse(Brushes.Green, x * (currentPositionPlayer - widthPlatform), startPositionPlayer * y,
            //                    x * widthPlatform, y * 3);


            if (isFirst)
                g.FillRectangle(Brushes.Black, ((this.Width - size) / 2) + x * currentPositionRival, startPositionRival * y,
                                x * widthPlatform, y * 3);
            //g.FillEllipse(Brushes.SteelBlue, x * currentPositionRival , startPositionRival * y,
            //                    x * widthPlatform, y * 3);
            else
                g.FillRectangle(Brushes.Black, ((this.Width - size) / 2) + x * (currentPositionRival - widthPlatform), startPositionRival * y,
                            x * widthPlatform, y * 3);
            //g.FillEllipse(Brushes.SteelBlue, x * currentPositionRival , startPositionRival * y,
            //                    x * widthPlatform, y * 3);

            g.FillEllipse(Brushes.Red, ((this.Width - size) / 2) + x * positionBallX, y * positionBallY, x * 3, y * 3);

            //for (int i = 0, j = 0; i < rows && j < cols;)
            //{

            //    if (gameField[i, j] == 1)
            //    {

            //        g.FillRectangle(Brushes.Black, x * j, y * i, x, y);
            //    }
            //    j++;
            //    if (j == 100)
            //    {
            //        j = 0;
            //        i++;
            //    }
            //}
            bGraph.Render(e.Graphics);//copy to screen
            g.Dispose();
        }
    }
}
