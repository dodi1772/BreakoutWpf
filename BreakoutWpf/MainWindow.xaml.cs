using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BreakoutWpf
{
    public partial class MainWindow : Window
    {
        private Rectangle paddle = null;
        private Ellipse ball = null;
        private readonly List<Rectangle> bricks = new();

        private readonly DispatcherTimer gameTimer = new();
        private double ballVX = 200;
        private double ballVY = -200;
        private int score = 0;
        private int lives = 3;
        private bool isPlaying = false;

        private const double PaddleWidth = 120;
        private const double PaddleHeight = 16;
        private const double BallSize = 14;
        private const int BrickRows = 5;
        private const int BrickCols = 8;
        private const double BrickWidth = 80;
        private const double BrickHeight = 24;
        private const double BrickPadding = 6;
        private const double TopOffset = 40;

        private DateTime lastTick = DateTime.Now;

        public MainWindow()
        {
            InitializeComponent();
            SetupCanvasSize();
            CreatePaddle();
            CreateBall();
            InitializeBricks();
            UpdateHud();
            //lefut a gameloop metodus
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameLoop;
        }

        private void SetupCanvasSize()
        {
            GameCanvas.Width = 800 - 16;
            GameCanvas.Height = 600 - 16;
        }

        private void CreatePaddle()
        {
            paddle = new Rectangle
            {
                Width = PaddleWidth,
                Height = PaddleHeight,
                Fill = Brushes.White,
                RadiusX = 4,
                RadiusY = 4
            };
            GameCanvas.Children.Add(paddle);
            Canvas.SetLeft(paddle, (GameCanvas.Width - PaddleWidth) / 2);
            Canvas.SetTop(paddle, GameCanvas.Height - PaddleHeight - 12);
        }

        private void CreateBall()
        {
            ball = new Ellipse
            {
                Width = BallSize,
                Height = BallSize,
                Fill = Brushes.LightBlue
            };//hozzaadom
            GameCanvas.Children.Add(ball);
            //alaphelyzet
            ResetBall();
        }

        private void ResetBall()
        {
            double paddleX = Canvas.GetLeft(paddle);
            double paddleY = Canvas.GetTop(paddle);
            Canvas.SetLeft(ball, paddleX + (PaddleWidth - BallSize) / 2);
            Canvas.SetTop(ball, paddleY - BallSize - 2);
            //alapsebesseg
            ballVX = 180;
            ballVY = -200;
        }

        private void InitializeBricks()
        {
            //meglevo brickek torlese
            foreach (var b in bricks)
            {
                GameCanvas.Children.Remove(b);
            }
            bricks.Clear();

            double totalWidth = BrickCols * BrickWidth + (BrickCols - 1) * BrickPadding;
            double startX = (GameCanvas.Width - totalWidth) / 2;
            //gridbe helyezzuk a bricks elemeket
            for (int row = 0; row < BrickRows; row++)
            {
                for (int col = 0; col < BrickCols; col++)
                {
                    var rect = new Rectangle
                    {
                        Width = BrickWidth,
                        Height = BrickHeight,
                        Fill = BrickColorForRow(row),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    double x = startX + col * (BrickWidth + BrickPadding);
                    double y = TopOffset + row * (BrickHeight + BrickPadding);
                    GameCanvas.Children.Add(rect);
                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, y);
                    bricks.Add(rect);
                }
            }
        }
        //sorok szin szerint
        private Brush BrickColorForRow(int row)
        {
            return row switch
            {
                0 => Brushes.OrangeRed,
                1 => Brushes.Orange,
                2 => Brushes.Yellow,
                3 => Brushes.GreenYellow,
                _ => Brushes.LightGreen,
            };
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlaying)
            {
                isPlaying = true;
                lastTick = DateTime.Now;
                gameTimer.Start();
                GameCanvas.Focus();
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            StopAndReset();
            StartButton_Click(sender, e);
        }

        private void StopAndReset()
        {
            gameTimer.Stop();
            isPlaying = false;
            score = 0;
            lives = 3;
            UpdateHud();
            InitializeBricks();
            Canvas.SetLeft(paddle, (GameCanvas.Width - PaddleWidth) / 2);
            Canvas.SetTop(paddle, GameCanvas.Height - PaddleHeight - 12);
            ResetBall();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            double elapsed = (now - lastTick).TotalSeconds;
            lastTick = now;

            UpdateBall(elapsed);
        }

        private void UpdateBall(double dt)
        {
            if (!isPlaying) return;

            double x = Canvas.GetLeft(ball);
            double y = Canvas.GetTop(ball);
            //labda idoalap
            x += ballVX * dt;
            y += ballVY * dt;
            //falakkal utkozes
            if (x <= 0)
            {
                x = 0;
                ballVX = Math.Abs(ballVX);
            }
            else if (x + BallSize >= GameCanvas.Width)
            {
                x = GameCanvas.Width - BallSize;
                ballVX = -Math.Abs(ballVX);
            }

            if (y <= 0)
            {
                y = 0;
                ballVY = Math.Abs(ballVY);
            }
            //ai
            var paddleRect = new Rect(Canvas.GetLeft(paddle), Canvas.GetTop(paddle), paddle.Width, paddle.Height);
            var ballRect = new Rect(x, y, BallSize, BallSize);

            if (ballRect.IntersectsWith(paddleRect) && ballVY > 0)
            {
                ballVY = -Math.Abs(ballVY);

                double hitPos = (x + BallSize / 2) - (Canvas.GetLeft(paddle) + paddle.Width / 2);
                double norm = hitPos / (paddle.Width / 2);
                ballVX = norm * 300;
                y = Canvas.GetTop(paddle) - BallSize - 0.5;
            }

            for (int i = bricks.Count - 1; i >= 0; i--)
            {
                var b = bricks[i];
                var bRect = new Rect(Canvas.GetLeft(b), Canvas.GetTop(b), b.Width, b.Height);
                if (ballRect.IntersectsWith(bRect))
                {
                    GameCanvas.Children.Remove(b);
                    bricks.RemoveAt(i);
                    score += 10;
                    UpdateHud();

                    ballVY = -ballVY;

                    ballVX *= 1.02;
                    ballVY *= 1.02;
                    break;
                }
            }
            //ai vege
            //utkozes a padloval
            if (y + BallSize >= GameCanvas.Height)
            {
                lives--;
                UpdateHud();
                if (lives <= 0)
                {
                    gameTimer.Stop();
                    isPlaying = false;
                    MessageBox.Show($"Game Over! Pontok: {score}");
                    return;
                }
                else
                {
                    Canvas.SetTop(paddle, GameCanvas.Height - PaddleHeight - 12);
                    ResetBall();
                    return;
                }
            }

            if (bricks.Count == 0)
            {
                gameTimer.Stop();
                isPlaying = false;
                MessageBox.Show($"Nyertél! Pontok: {score}");
                return;
            }

            Canvas.SetLeft(ball, x);
            Canvas.SetTop(ball, y);
        }

        private void UpdateHud()
        {
            ScoreText.Text = score.ToString();
            LivesText.Text = lives.ToString();
        }

        private void GameCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //eger a palyan
            Point p = e.GetPosition(GameCanvas);
            //paddle a palyan
            double x = p.X - paddle.Width / 2;

            x = Math.Max(0, Math.Min(GameCanvas.Width - paddle.Width, x));
            Canvas.SetLeft(paddle, x);
        }
    }
}