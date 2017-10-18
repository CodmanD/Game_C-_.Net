using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace kursServer
{
    class Program
    {
        static List<Game> games = new List<Game>();
       // static List<String> listPlayers = new List<String>();//игроки из базы 
        static List<String> listPlayersOnline = new List<String>();//игроки онлайн
       // static List<String> listPlayersPass = new List<String>();
       // static List<String> listPlayersId = new List<String>();
       // static List<Player> lPlayers = new List<Player>();
        static Mutex mutex = new Mutex();
        static DbConnection cn;
        static DbCommand cmd;
        class Player //класс для игрока
        {
            public String id{get;set;}
            public String name { get; set; }
            public String pass { get; set; }

            public String email { get; set; }
            public String gender { get; set; }
            

            public int positionHorizontal { get; set; }

           public int positionVertical { get; set; }

            public bool moveRight { get; set; }
            public bool moveLeft { get; set; }
            public bool isMove { get; set; }
            public int width { get; set; }

            public int goals=0;
            public TcpClient clientSlave { get; set; }
        }

        class Game  
        {
           
           public  int gfRows = 100;
           public  int gfCols = 100;
            public int[,] gameField;//= new int[gfRows, gfCols];
            //public List<Player> players = new List<Player>();
            public int status = 1;//1-игра создана 2-добавлен второй игрок 3- мяч в игре 4-пауза 5- победа 1 игрока 6 - победа второго
            public Player player1;
            public Player player2;
            private int maxGoals = 5;//Игра до 10 голов
            bool addRes =true;
            public Mutex m = new Mutex();
           
            private Random rand = new Random();
            Thread ThrPositionsTo = null;
            public Game()//Конструктор игры
            {
               
                gameField = new int[this.gfRows, this.gfCols];
                positionBallX = this.gfCols / 2;
                positionBallY = this.gfRows / 2;
                ThrPositionsTo = new Thread(this.lastGameGo);
                ThrPositionsTo.IsBackground = true;
                ThrPositionsTo.Start();
               // positionBallX = player1.positionHorizontal + player1.width / 2;
                //positionBallY = player1.positionVertical - 3;
            }
         

          

           
            public  void SendToClientNameOpponent()//отсылка имени соперника
            {
                //  Console.WriteLine("For Player1");
                try
                {
                    byte[] bytes = new byte[player2.name.Length + 2];
                    byte[] nameB=Encoding.UTF8.GetBytes(player2.name);
                    bytes[0] =(byte)0;
                    bytes[1] =(byte)player2.name.Length;
                    for (int i = 2,j=0; i < bytes.Length; i++)
                    {
                        bytes[i] = (nameB[j++]);
                    }
                   
                NetworkStream NS = null;
                
                    NS = player1.clientSlave.GetStream();
                    NS.Write(bytes, 0, bytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Thread Master message :SentToClientNameError " + ex.Message);
                    this.status = 4;

                }
            }

            void addResultatsInDB()
            {try
                {
                    if (addRes)
                    {
                        cmd.CommandText = "Insert INTO Resultats (id_p1,goals_p1,id_p2,goals_p2) values ('" + player1.id.ToString() +
                            "','" + player1.goals + "','" + player2.id.ToString() + "','" + player2.goals + "')";
                        int k = cmd.ExecuteNonQuery();

                        if (k == 1)
                        {
                            Console.WriteLine("Добавлен результат");
                            addRes = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            } 
            //-----------Отсыдка клиенту позиций----------------
            private void SendToClientPositions()
            {
                //  Console.WriteLine("For Player1");
                byte[] bytes=null;
                try
                {
                    if(player1!=null&&player2!=null)
                     bytes =new Byte[] {Convert.ToByte( 1),
                                Convert.ToByte( this.status),
                                Convert.ToByte( this.positionBallX),
                                Convert.ToByte( this.positionBallY),
                                Convert.ToByte( this.player1.positionHorizontal),
                                Convert.ToByte( this.player2.positionHorizontal),
                                Convert.ToByte( this.player1.goals),
                                Convert.ToByte( this.player2.goals)};
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BYTES [] ERROR: "+ex.Message);
                }
                NetworkStream NS=null;
                try
                {
                    NS = player1.clientSlave.GetStream();
                    NS.Write(bytes, 0, bytes.Length);
                }
                catch (Exception ex)
                {
                    mutex.WaitOne();
                    if (this.status == 5)
                    {
                       
                        // GameTheEnd(player1);
                        if (games.Contains(this))
                        {
                            addResultatsInDB();
                            games.Remove(this);
                        }
                       
                    }
                    //   Console.WriteLine("Thread Master message: for p1" + ex.Message+"\tStatus ="+status);

                    this.status = 6;
                    if (player2 != null)
                        player2.goals = 5;
                    
                        if (player1!=null)
                        listPlayersOnline.Remove(player1.name);
                    mutex.ReleaseMutex();

                }
              //  Console.WriteLine("For Player1 end");
              if(player2!=null)
                try
                {
                   NS = player2.clientSlave.GetStream();
                   NS.Write(bytes, 0, bytes.Length);
                }
                catch (Exception ex)
                {
                        mutex.WaitOne();
                        if (this.status == 6)
                    {
                            // GameTheEnd(player2);
                            if (games.Contains(this))
                            {
                                addResultatsInDB();
                                games.Remove(this);
                            }
                    }
                       // Console.WriteLine("Thread Master message: for p2" + ex.Message + "\tStatus =" + status);
                        this.status = 5;
                        player1.goals = 5;
                        if(player2!=null)
                        listPlayersOnline.Remove(player2.name);
                        mutex.ReleaseMutex();
                }
              //  Console.WriteLine("For Player2 end");

              //  if (NS != null)
                //    NS.Close();
            }
            // -------------Конец метода отсылки позиций

            
            public int positionBallX ;
            public int positionBallY ;
            public int widthBall = 3;
            private bool ballX = true;
            private bool ballY = false;
            private int kX = 1;//коэффициент отражения по X
            private int kY = 1;//коэффициент отражения по Y
            public bool moveBall=false;
            private int k = 2;//коэффициент движения платформы

            //---Новая версия изменений в игре--------

           
            //-----------LastGameGo
            public void lastGameGo()
            {
                int delay = 30;
                while (true)
                {
                    this.m.WaitOne();
                    //  Console.WriteLine("Game status= "+ this.status);
                    if (this.status == 2|| status == 5 || status == 6)
                    {
                       // Console.WriteLine("Game status= " + this.status);
                        SendToClientPositions();
                        if (status == 5 || status == 6)
                        { status = 7; moveBall = false; }
                    }
                    //if (this.status >= 3&&this.status!=4)

                    if(this.status==3)
                    {
                       // Console.WriteLine("Game degin");
                        try
                        {
                            if (moveBall)
                            {


                                if (ballY && positionBallY < gfRows - 1)
                                {
                                    positionBallY += kY;
                                   // Console.WriteLine("Летим  вниз");
                                }
                                else
                              //  if (positionBallY >= 1)
                                {
                                    positionBallY -= kY;
                                   // Console.WriteLine("Летим вверх");
                                }

                                if (ballX && positionBallX < gfCols - (1 + widthBall))
                                {
                                    positionBallX += kX;
                                    //Console.WriteLine("Летим вправо");
                                }
                                else
                                  //  if (positionBallX >= kX)
                                {
                                    positionBallX -= kX;
                                    //Console.WriteLine("Летим влево");
                                }


                                if (positionBallX >= gfCols - (1 + widthBall) || positionBallX <= 0)
                                {
                                    if (ballX)
                                    {
                                        positionBallX -= kX;
                                        //positionBallX -= kX;
                                        ballX = false;
                                    }
                                    else
                                    {
                                        positionBallX += kX;
                                        //positionBallX += kX;
                                        ballX = true;
                                    }
                                    // Console.WriteLine("position Ball X=" + positionBallX + "\tY= " + positionBallY);

                                }

                                //Проверки касаний платформы 1 player
                                //Проверки на касаний торцов
                                if ((positionBallX + widthBall >= player1.positionHorizontal  //левый торец
                                     && positionBallX + widthBall <= player1.positionHorizontal + kX)
                                   && (positionBallY + widthBall == player1.positionVertical) && ballX)
                                {

                                    // kX = 3;
                                    kX++;
                                    ballX = false;
                                    ballY = false;
                                  //  Console.WriteLine("Player1:Left torec");
                                }
                                if ((positionBallX == player1.positionHorizontal + player1.width//правый торец
                                && positionBallX == player1.positionHorizontal + player1.width - kX)
                                 && (positionBallY + widthBall == player1.positionVertical) && !ballX)
                                {
                                    // kX = 3;
                                    kX++;
                                    ballY = false;
                                    ballX = true;
                                   // Console.WriteLine("Player1:Rigth torec");
                                }
                                //Проверка на касание внутри платформы

                                //if (positionBallY + widthBall == player1.positionVertical
                                //    && positionBallX >= player1.positionHorizontal
                                //    && positionBallX <= player1.positionHorizontal + player1.width / 2 - player1.width / 10)//попадание в правую часть 

                                if ((positionBallY + widthBall == player1.positionVertical)
                                    && (positionBallX +widthBall > player1.positionHorizontal + player1.width / 2
                                    && positionBallX <= player1.positionHorizontal + player1.width))
                                {
                                  //  Console.WriteLine("Player1:/попадание в правую часть ");
                                    ballY = false;
                                    if (ballX)
                                        kX++;
                                    //ballX = false;
                                    else
                                        kX--;
                                }

                                //if (positionBallY + widthBall == player1.positionVertical
                                //    && positionBallX >= player1.positionHorizontal + player1.width / 2 + player1.width / 10
                                //    && positionBallX <= player1.positionHorizontal + player1.width )//попадание в левую часть 
                                if (positionBallY + widthBall == player1.positionVertical
                                    && (positionBallX + widthBall > player1.positionHorizontal
                                    && positionBallX + widthBall <= player1.positionHorizontal + player1.width / 2))
                                {
                                  //  Console.WriteLine("Player1:/попадание в левую часть  ");
                                    ballY = false;
                                    if (ballX)
                                        kX--;

                                    else
                                        kX++;
                                    //ballX = true;
                                }
                               


                                //----------------------Проверка касаний платформы 2 player----------------
                                //проверка касаний торцов

                                //if ((positionBallX + widthBall == player2.positionHorizontal) &&
                                //       (positionBallY + 1 == player2.positionVertical+widthBall
                                //       || positionBallY + 2 == player2.positionVertical+widthBall
                                //       || positionBallY + 3 == player2.positionVertical+widthBall) && ballX)
                                if ((positionBallX + widthBall >= player2.positionHorizontal) &&
                                    (positionBallX + widthBall <= player2.positionHorizontal + kX)
                                    && (positionBallY == player2.positionVertical + widthBall)
                                    && ballX)
                                {
                                  //  Console.WriteLine("Player2:Left torec");
                                    //kX = 3;
                                    kX++;
                                    ballX = false;
                                    ballY = true;
                                }
                                //if ((positionBallX == player2.positionHorizontal + player2.width) &&
                                //    (positionBallY + 1 == player2.positionVertical + widthBall
                                //    || positionBallY + 2 == player2.positionVertical + widthBall
                                //    || positionBallY + 3 == player2.positionVertical + widthBall) && !ballX)

                                if ((positionBallX <= player2.positionHorizontal + player2.width - kX) &&
                                    (positionBallX >= player2.positionHorizontal + player2.width)
                                    && (positionBallY == player2.positionVertical + widthBall) && !ballX)
                                {
                                   //Console.WriteLine("Player2:Rigth torec");
                                    // kX = 3;
                                    kX++;
                                    ballX = true;
                                    ballY = true;
                                }
                                //Проверка на касание внутри платформы

                                //if (positionBallY  == player2.positionVertical+widthBall
                                //    && positionBallX >= player2.positionHorizontal+ player2.width / 2 + player2.width / 10
                                //    && positionBallX <= player2.positionHorizontal + player2.width )//попадание в правую часть 
                                if (positionBallY == player2.positionVertical + widthBall &&
                                    positionBallX >= player2.positionHorizontal + player2.width / 2
                                    && positionBallX < player2.positionHorizontal + player2.width)
                                {
                                  //  Console.WriteLine("Player2:попадание в правую часть ");
                                    ballY = true;
                                    if (ballX)
                                        kX++;
                                    else
                                        kX--;
                                    //ballX = true;
                                }

                                //if (positionBallY  == player2.positionVertical + widthBall
                                //    && positionBallX >= player2.positionHorizontal 
                                //    && positionBallX <= player2.positionHorizontal + player2.width / 2 - player2.width / 10)//попадание в левую часть 
                                if (positionBallY == player2.positionVertical + widthBall
                                    && positionBallX + widthBall > player2.positionHorizontal
                                    && positionBallX  < player2.positionHorizontal + player2.width / 2)
                                {
                                   // Console.WriteLine("Player2:попадание в левую часть  ");
                                    ballY = true;
                                    if (ballX)
                                    {
                                        kX--;
                                        // ballX = false;
                                    }
                                    else
                                        kX++;
                                }

                            }
                            else
                            {
                                if (positionBallY + widthBall == player1.positionVertical)
                                    positionBallX = player1.positionHorizontal + player1.width / 2 - widthBall / 2;//Устанавливаем шарик 
                                else                     //на серединку платформы и перемещаем его соответсвенно
                                    positionBallX = player2.positionHorizontal + (player2.width / 2) + widthBall / 2;
                            }

                            
                           
                           

                            int tmpPosition = player1.positionHorizontal;

                                if (this.player1.isMove && this.player1.moveLeft)
                                {
                                    this.MoveLeft(player1, ref tmpPosition);
                                    //   Console.WriteLine("Player " + player1.name + "\tNow:X= " + player1.positionHorizontal + "\tY= " + player1.positionVertical);
                                }// this.MoveLeftPlayer(player1.positionVertical, ref tmpPosition);
                                if (this.player1.isMove && this.player1.moveRight)
                                {
                                    this.MoveRight(player1, ref tmpPosition);
                                    // Console.WriteLine("Player " + player1.name + "\tNowt:X= " + player1.positionHorizontal + "\tY= " + player1.positionVertical);
                                }//this.MoveRightPlayer(player1.positionVertical, ref tmpPosition);

                                player1.positionHorizontal = tmpPosition;
                                if (this.player2 != null)
                                {
                                    tmpPosition = player2.positionHorizontal;
                                    if (this.player2.isMove && this.player2.moveLeft)
                                        // this.MoveLeft2(player2.positionVertical, ref tmpPosition);
                                        this.MoveRight(player2, ref tmpPosition);
                                    if (this.player2.isMove && this.player2.moveRight)
                                        this.MoveLeft(player2, ref tmpPosition);
                                    //this.MoveRight2(player2.positionVertical, ref tmpPosition);

                                    player2.positionHorizontal = tmpPosition;
                                    //Console.WriteLine("Position p1 =" + player1.currentPosition + "\tPosition p2 = " + player2.currentPosition);
                                } // Console.WriteLine("Thread Ball release mutex");
                            

                            //Проверки на вылет шара

                            if (positionBallY <= 0)
                            {
                                positionBallX = player1.positionHorizontal + player1.width / 2;
                                positionBallY = player1.positionVertical - widthBall;//цифра рамзмер мячика
                                this.moveBall = false;
                                this.ballY = false;
                                kX = 1;
                                //Console.WriteLine("шарик вылетел");
                                player1.goals++;
                                if (player1.goals == maxGoals)
                                {
                                    this.status = 5;
                                    //addResultatsInDB();
                                    //GameTheEnd(player1);
                                   // Console.WriteLine("P1 win");
                                }
                                
                            }
                            if (positionBallY >= gfRows - 1)
                            {
                                positionBallX = player2.positionHorizontal + (player2.width / 2);
                                positionBallY = player2.positionVertical + widthBall;//цифра рамзмер мячика
                                this.moveBall = false;
                                this.ballY = true;
                                kX = 1;
                               // Console.WriteLine("шарик вылетел");
                                player2.goals++;
                                if (player2.goals == maxGoals)
                                {
                                    this.status = 6;
                                   // addResultatsInDB();
                                    // GameTheEnd(player2);
                                    // Console.WriteLine("P2 win");
                                }
                                
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        //if(status==2||status==3)
                        SendToClientPositions();//Отправляем клиентам точки координат.
                    }
                    if (this.status == 5 || this.status == 6)
                        addResultatsInDB();
                    this.m.ReleaseMutex();
                    //  Console.WriteLine("Sleep = "+delay+"ms");
                   // Thread.Sleep(delay);
                    Thread.Sleep(30);
                }

            }


            public void MoveRight(Player p, ref int tmpPosition)
            {
                if (tmpPosition <= gfCols - p.width-k)
                    tmpPosition += k;
            }
            public void MoveLeft(Player p, ref int tmpPosition)
            {  
                if (tmpPosition >=k )
                    tmpPosition -= k;
            }

            //public void GameTheEnd(Player p)
            //{
            //    if(p!=null)
            //   // p.goals = 5;
            //    if(status==5||status==6)
            //    addResultatsInDB();
            //}
        }

    class ThrReadWriter
        {
            private TcpClient clientMaster;
            private TcpClient clientSlave;


            private Game game;

            private Player player;

            // static String clientName;
            // static String clientPass;

            // String clientId;
            //Thread thrGame;
           // String strProvider;
           // String strConString;

            public ThrReadWriter( TcpClient clientMaster, TcpClient clientSlave)
            {
                this.clientMaster = clientMaster;
                this.clientSlave = clientSlave;
                // strProvider = ConfigurationManager.AppSettings["provider"];
                // strConString = ConfigurationManager.AppSettings["conString"];

                //DbProviderFactory Factory = DbProviderFactories.GetFactory(strProvider);
                //cn = Factory.CreateConnection();
                //cmd = cn.CreateCommand();
                //cn.ConnectionString = strConString;

            }
            public void run()//основной поток для клиента и получение от него приказов
            {
               // Console.WriteLine("Сервер Slave в работе " + clientId);
                String strRemoteEndPoint = clientMaster.Client.RemoteEndPoint.ToString();
                MemoryStream MS = new MemoryStream();
                while (true)
                {
                    try
                    {
                       
                        byte[] buf = new byte[128];
                        NetworkStream NS = clientMaster.GetStream();
                        while (true)
                        {
                            //-----Читаем запрос от клиента---------
                            while (true)//Пока есть данные для чтения
                            {
                                int cnt = NS.Read(buf, 0, buf.Length);
                                if (cnt == 0) throw new Exception("Получено 0 байт");
                                MS.Write(buf, 0, cnt);
                                if (!NS.DataAvailable) break;
                            }
                            //----Обработка запроса---------------------------------
                            byte[] a = MS.ToArray();

                            String msg = Encoding.UTF8.GetString(a, 0, a.Length);
                            Console.WriteLine("Получено от клиента {0} : {1} ", strRemoteEndPoint, msg);

                            mutex.WaitOne();
                            msg = this.handleRequest(msg);
                            mutex.ReleaseMutex();

                            Console.WriteLine("Шлем клиенту {0}  ", msg);
                            //----Ответ клиенту----------------------------
                          
                            a = Encoding.UTF8.GetBytes(msg);
                            NS.Write(a, 0, a.Length);
                            //Очистка MemoryStream-----
                            MS.SetLength(0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Разрыв соединения :{0} ", strRemoteEndPoint);
                        mutex.WaitOne();
                        clientMaster.Close();
                        clientSlave.Close();
                        if (games.Contains(this.game))
                        {
                            //  if (game != null)
                            game.m.WaitOne();
                                game.status = (player.name==game.player1.name)?5:6;
                            game.m.ReleaseMutex();
                        }
                        if (this.player != null)
                            listPlayersOnline.Remove(this.player.name);
                        if (game != null)
                            game = null;
                        //game.players.Remove(clientName);
                        //clientSlave.Close();
                       mutex.ReleaseMutex();
                        break;
                    }
                    Thread.Sleep(20);
                }
            }

            private bool ContainsNameInDB(string name,string pass)//метод для поиска игрока в БД
            {
                int i = 0;
                try
                {
                    //String strProvider = ConfigurationManager.AppSettings["provider"];
                    //String strConString = ConfigurationManager.AppSettings["conString"];

                    //DbProviderFactory Factory = DbProviderFactories.GetFactory(strProvider);
                    //cn = Factory.CreateConnection();

                   //cmd = cn.CreateCommand();
                    //cn.ConnectionString = strConString;
                    //cn.Open();

                    cmd.CommandText = "SELECT * FROM Players Where name='" + name + "' AND pass='" + pass + "';";
                    DbDataReader R = cmd.ExecuteReader();
                   
                    while (R.Read())
                    {
                        i++;
                    }
                    R.Close();
                   // cn.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                    if (i >= 1)
                        return true;
                    else
                        return false;
                

                
               }

            private bool ContainsNameOnline(string name)//метод для поиска имени 
            {
               foreach(string n in listPlayersOnline)
                    if(n==name)
                return true;
                return false;
            }

            private Player getPlayerFromDB(String name, string pass)
            {
               // Console
                //cn.Open();
                cmd.CommandText = "SELECT * FROM Players Where name='"+name+"' AND pass='"+pass+"';";
                DbDataReader R = cmd.ExecuteReader();
                Player p = new Player();
                int i = 0;
                while (R.Read())
                {
                    p.id = (R["Id"].ToString());
                    p.name = (R["Name"].ToString());
                     p.pass = (R["Pass"].ToString());
                   
                    p.email = (R["email"].ToString());
                    p.gender = (R["gender"].ToString());
                    i++;
                }
                R.Close();
               // cn.Close();
                Console.WriteLine(p.name + " created");
                if (i == 1)
                    return p;
                else
                    return null;
            }
            //----------------Обработка запросов клиента -----------------------------------------------------
            protected virtual String handleRequest(String msg)//связь с клиентом 
            {
                try
                {
                    String[] z = msg.Split(new char[] { '|' });
                    Console.WriteLine("Пришло от клиента " + msg);
                    //if (z.Length != 3)
                    //    return "ERROR|Неправильный формат запроса";

                    switch (z[0])
                    {
                        case "LOGIN":
                            // m.WaitOne();
                           
                                //пытаемся добавиться в игру 
                                if (!ContainsNameOnline(z[1])&& ContainsNameInDB(z[1], z[2]))
                                {
                                    this.player = getPlayerFromDB(z[1], z[2]);
                                    listPlayersOnline.Add(player.name);
                                    return "LOGINOK|" + this.player.pass + "|" + this.player.email + "|" + player.gender;

                                }
                                else
                                    return "LOGINERROR|Проверьте правильность ввода или зарегистрируйтесь";
                                break;
                            
                        case "REGISTRY":
                            Console.WriteLine("Регистрация нового пользователя");
                            if (!ContainsNameInDB(z[1],z[2]))
                            {
                                try
                                {
                                    //Console.WriteLine("Регистрация нового пользователя такого в базе нет");
                                   int k=AddInDB(z[1], z[2], z[3],z[4]);
                                    //Console.WriteLine("Добавлен пользователь " + z[1] + " Pass " + z[2]);
                                    //Thread.Sleep(10);
                                    Console.WriteLine(k.ToString());
                                    if (k == 1)
                                    {
                                        this.player = getPlayerFromDB(z[1], z[2]);
                                    }
                                    //Console.WriteLine("Создан игрок пользователь " + z[1] + " Pass " + z[2]);
                                    //if (this.player == null) { }
                                    //   Console.WriteLine("USER IS NULL ");
                                }
                                catch (Exception ex)
                                {
                                   // Console.WriteLine("Чота с базой");

                                }
                                if (player != null)
                                {
                                    Console.WriteLine("Добавлен в игру");
                                    listPlayersOnline.Add(player.name);
                                }
                                else

                                    return "REGISTRYERROR|" + z[1];
                                //Console.WriteLine(String.Format("New User{0} add in DB", z[1]));
                                // games.Add(game);

                                // m.ReleaseMutex();
                                //return "REGISTRYOK|" + game.gfRows + "|" + game.gfCols + "|" + game.status ;
                                return "REGISTRYOK|" +player.name+"|"+ this.player.pass + "|" + this.player.email + "|" + player.gender; 
                                // return "LOGINOK|" + z[1];

                            }
                            else
                            {
                                // m.ReleaseMutex();
                                Console.WriteLine(String.Format("REGISTRYERROR|Такой Пользователь  {0} зарегистрирован.", z[1]));
                                return "REGISTRYERROR|" + z[1];
                            }


                            break;

                        case "StartGame":
                            //for (int j=games.Count-1;j>=0;j--)
                            // {
                            //    if (games[j].status == 7)
                            //        games.RemoveAt(j);
                            //    games[j] = null;
                            //}
                                foreach (Game g in games)
                            {
                                if (g.status == 1)
                                {    game = g;
                                        this.player.isMove = false;
                                        this.player.positionHorizontal = game.gfCols / 2 - (game.gfCols / 5 / 2);
                                        this.player.positionVertical = 10;
                                        this.player.moveLeft = false;
                                        this.player.moveRight = false;

                                        this.player.width = game.gfCols / 5;
                                        this.player.clientSlave = this.clientSlave;
                                        game.player2 = this.player;
                                        g.status = 2;

                                        Console.WriteLine(String.Format("User{0}add in game", z[1]));

                                        //  m.ReleaseMutex();
                                        //return "LOGINOK|" + z[1];
                                        this.game.SendToClientNameOpponent();
                                    return "StartGameOK|Two|" + game.gfRows + "|" + game.gfCols + "|" + game.status+"|"+game.player1.name ;
                                    break;
                                }
                            }
                            //Создаем игру для первого игрока
                               
                                game = new Game();
                                this.player.positionHorizontal = game.gfCols / 2 - (game.gfCols / 5 / 2);
                                this.player.positionVertical = game.gfRows - 10;
                                this.player.moveLeft = false;
                                this.player.moveRight = false;
                                this.player.isMove = false;

                                this.player.width = game.gfCols / 5;
                                this.player.clientSlave = this.clientSlave;


                                game.player1 = this.player;
                                game.positionBallX = game.player1.positionHorizontal + game.player1.width / 2;
                                game.positionBallY = game.player1.positionVertical - 3;
                                game.status = 1;
                                // game.players.Add(p);
                                Console.WriteLine(String.Format("User{0}created  game", z[1]));
                                games.Add(game);
                            return "StartGameOK|One| " + game.gfRows + " | " + game.gfCols + " | " + game.status;
                            break;
                        case "Update":
                           int i= UpdateDB(z[1],z[2],z[3],z[4]);
                            if (i == 1)
                                return "UpdateOK|";
                            break;
                        case "Resultats":
                            //  m.WaitOne();
                            {
                                Console.WriteLine();
                                msg = "Resultats|";
                               String query=" Select Ps1.name as P1,  Resultats.goals_p1 as goalsP1 ,Ps2.name as P2,Resultats.goals_p2 as goalsP2 From Resultats ,Players as Ps1,Players as Ps2 Where Ps1.id = Resultats.id_p1 And Ps2.id = Resultats.id_p2 And(Ps1.name = '"
                                    +player.name+"' Or  Ps2.name = '"+player.name+"')";
                                Console.WriteLine(query);
                                cmd.CommandText = query;
                                DbDataReader R = cmd.ExecuteReader();

                                while (R.Read())
                                {
                                  String p1=(R["P1"].ToString());
                                    String goalsP1 = (R["goalsP1"].ToString());
                                    String p2 = (R["P2"].ToString());
                                    String goalsP2 = (R["goalsP2"].ToString());
                                    msg = msg + p1 + " " + goalsP1 + ":" + goalsP2 + " " + p2 + "|";
                                    //Console.WriteLine("AddPlayer");
                                }
                                R.Close();
                                //Console.WriteLine(msg);
                                return msg;
                            }
                            //m.ReleaseMutex();
                            break;

                        case "BestPlayers":
                            //  m.WaitOne();
                            {
                                msg = "Resultats|";
                                String query = " Select Ps1.name as P1,  Resultats.goals_p1 as goalsP1 ,Ps2.name as P2,Resultats.goals_p2 as goalsP2 From Resultats ,Players as Ps1,Players as Ps2 Where Ps1.id = Resultats.id_p1 And Ps2.id = Resultats.id_p2 And(Ps1.name = '"
                                     + player.name + "' Or  Ps2.name = '" + player.name + "')";

                                cmd.CommandText = query;
                                DbDataReader R = cmd.ExecuteReader();

                                while (R.Read())
                                {

                                    //String id=
                                    String p1 = (R["P1"].ToString());
                                    String goalsP1 = (R["goalsP1"].ToString());
                                    String p2 = (R["P2"].ToString());
                                    String goalsP2 = (R["goalsP2"].ToString());
                                    msg = msg + p1 + " " + goalsP1 + ":" + goalsP2 + " " + p2 + "|";
                                    //Console.WriteLine("AddPlayer");

                                }
                                R.Close();
                                //Console.WriteLine(msg);
                                return msg;
                            }
                            //m.ReleaseMutex();
                            break;
                        case "Move":
                            //Console.WriteLine("Прилетело Move |  " + z[1]);
                            switch (z[1])
                            {
                                case "right":
                                    game.m.WaitOne();
                                    this.player.isMove = true;
                                    this.player.moveLeft = false;
                                    this.player.moveRight = true;
                                    //game.MoveRight();
                                    game.m.ReleaseMutex();
                                    break;
                                case "left":
                                    game.m.WaitOne();
                                    this.player.isMove = true;
                                    this.player.moveRight = false;
                                    this.player.moveLeft = true;
                                    //game.MoveLeft();
                                    game.m.ReleaseMutex();
                                    break;

                                case "stop":
                                    game.m.WaitOne();
                                    this.player.isMove = false;
                                    this.player.moveRight = false;
                                    this.player.moveLeft = false;
                                    game.m.ReleaseMutex();
                                    break;
                            }
                            break;
                        //  game.ShowField();
                        case "Game":
                            // Console.WriteLine("Прилетело Game|  " + z[1]);
                            switch (z[1])
                            {
                                case "start":
                                    game.m.WaitOne();
                                    if (game != null)
                                    {
                                        Console.WriteLine("Start Game");
                                        game.status = 3;
                                        game.moveBall = true;
                                    }
                                    game.m.ReleaseMutex();
                                    break;
                                case "pause":
                                    game.m.WaitOne();
                                    Console.WriteLine("PAUSE GAME");
                                    if (game.status == 3)
                                    {
                                        game.status = 4;
                                        game.m.ReleaseMutex();
                                        break;
                                    }
                                    if (game.status == 4)
                                    {
                                        game.status = 3;

                                    game.m.ReleaseMutex();
                                    break;
                                        }
                                    game.m.ReleaseMutex();
                                    break;
                                case "stopGame":
                                    Console.WriteLine("STOP GAME");
                                    game.m.WaitOne();
                                    

                                    if (player == game.player1)
                                    {
                                        Console.WriteLine("Player2 5 goals");
                                        game.status = 6;
                                        game.player2.goals = 5;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Player1 5 goals");
                                        game.status = 5;
                                        game.player1.goals = 5;
                                    }
                                   
                                        game.m.ReleaseMutex();
                                    break;
                            }
                            break;
                        default: return "ERROR|Неизвестная команда";
                    }
                    return z[1];
                }
                catch (Exception ex)
                {
                   // Console.WriteLine("Ошибка обработки запроса");
                    return "";
                }
            }

            private int AddInDB(string name, string pass, string email,string gender)
            {
                //AddFromDBInListUsers();
                if (!ContainsNameInDB(name,pass))
                {

                    // Console.WriteLine("Query:" + "Insert INTO Players(name,pass,email) values('" + name + "','" + pass + "','" + email + "')");
                    cmd.CommandText = "Insert INTO Players(name,pass,email,gender) values ('" + name + "','" + pass + "','" + email + "','" + gender + "')";
                    int k = cmd.ExecuteNonQuery();
                    
                    return k;
                }
                return 0;
              
            }

            private int UpdateDB(string name, string pass, string email, string gender)
            {
                //AddFromDBInListUsers();
             
              
                    cmd.CommandText = "UPDATE Players Set name='"+name+"',pass='"+pass+"',email='"+email+"',gender='"+gender+"' Where id="+this.player.id;
                Console.WriteLine("Update begin");
                int k = cmd.ExecuteNonQuery();
                Console.WriteLine("Update close");
                if (k == 1)
                    {
                        return 1;
                        Console.WriteLine("Изменен пользователь");
                    }
                    return 0;
                

            }

        }

        //===========end class ThrReadWriter==========
//==================Класс серверного потока ожидает запросы на соединение
        class ThreadServer
        {
           
            private TcpListener serverSlave;
            private TcpListener serverMaster;
            private bool isRun = true;
           
            public void run()
            {
                try
                {
                    string ip = System.Configuration.ConfigurationManager.AppSettings["Ip"];
                    this.serverSlave = new TcpListener(IPAddress.Parse(ip), 5000);
                    this.serverSlave.Start();//Начинаем прослушивание
                    this.serverMaster = new TcpListener(IPAddress.Parse(ip), 5001);
                    this.serverMaster.Start();//Начинаем прослушивание
                                              // string ip = System.Configuration.ConfigurationManager.AppSettings["Ip"];

                   String strProvider = ConfigurationManager.AppSettings["provider"];
                   String strConString = ConfigurationManager.AppSettings["conString"];

                    DbProviderFactory Factory = DbProviderFactories.GetFactory(strProvider);
                    cn = Factory.CreateConnection();
                   
                    cn.ConnectionString = strConString;
                    cmd = cn.CreateCommand();
                    cn.Open();
                    while (this.isRun)
                    {
                        Console.WriteLine("Ожидание запроса на установление соединения");
                        TcpClient clientMaster = this.serverSlave.AcceptTcpClient();

                        //Console.WriteLine("Подключение : {0} ", clientMaster.Client.RemoteEndPoint.ToString());
                        
                        TcpClient clientSlave = this.serverMaster.AcceptTcpClient();
                        //Console.WriteLine("Подключение : {0} ", clientSlave.Client.RemoteEndPoint.ToString());

                        ThrReadWriter RW = new ThrReadWriter(clientMaster,clientSlave);

                        Thread T1 = new Thread(RW.run);
                        T1.IsBackground = true;
                        T1.Start();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка серверного сокета:{0} ", ex.Message);
                }
            }

      public void stopServer()
            {
                this.serverMaster.Stop();
                this.serverSlave.Stop();
                this.isRun = false;
                cn.Close();
                cn.Dispose();
            }
            
        }

        

        static void Main(string[] args)
            {
            

            ThreadServer TS = new ThreadServer();
            Thread T = new Thread(TS.run);
            T.IsBackground = true;
            T.Start();
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(500);
            }
            TS.stopServer();
            Console.WriteLine("Good Bye");
        }


        
    }
}

