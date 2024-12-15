using Microsoft.VisualBasic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Mastermind
{
    public class GameState
    {
        public string PlayerName { get; set; }
        public int MaxAttempts { get; set; }
        public int Attempts { get; set; }
        public int Score { get; set; }
    };

    public partial class MainWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        private string[] colors = { "Red", "Yellow", "Orange", "White", "Green", "Blue" };
        private string[] secretCode;
        private List<Brush> ellipseColor = new List<Brush> { Brushes.Red, Brushes.Yellow, Brushes.Orange, Brushes.White, Brushes.Green, Brushes.Blue };
        private string[] _highscores = new string[15];
        // int attempts = 0;
        int countDown = 10;
        // int totalScore = 100;
        // private int _maxAttempts = 10;
        // private string currentPlayerName = string.Empty;
        private List<StackPanel> attemptPanels;
        private List<GameState> gameStates;
        private int currentPlayer;

        public MainWindow()
        {
            InitializeComponent();
            StartGame();
            UpdateTitle();
            StartCountDown();
        }

        private void StartGame()
        {
            
            currentPlayer = 0;
            gameStates = new List<GameState>();
            bool isAllPlayersAdded = false;

            while (!isAllPlayersAdded)
            {   
                GameState playerGameState = new GameState();

                // Get player name
                string playerName = string.Empty;
                while (string.IsNullOrWhiteSpace(playerName))
                {
                    playerName = Interaction.InputBox("Naam speler:", "Invoer");
                    if (string.IsNullOrWhiteSpace(playerName))
                    {
                        MessageBox.Show("Naam mag niet leeg zijn, vul uw naam in", "Geen naam", MessageBoxButton.OK);
                    }
                }

                // Get player number of attempts
                int attemptsInput = 0;
                bool validInput = false;
                while (!validInput)
                {
                    string input = Interaction.InputBox("Geef het maximaal aantal pogingen in (tussen 3 en 20):", "Maximale Pogingen", 10.ToString());
                    if (int.TryParse(input, out attemptsInput) && attemptsInput >= 3 && attemptsInput <= 20)
                    {
                        validInput = true;
                    }
                    else
                    {
                        MessageBox.Show("Voer een geldig getal in tussen 3 en 20.", "Ongeldige Invoer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }


                playerGameState.PlayerName = playerName;
                playerGameState.MaxAttempts = attemptsInput;
                playerGameState.Attempts = 0;
                playerGameState.Score = 100;
                gameStates.Add(playerGameState);

             
                if(MessageBox.Show("Wil je nog een speler toevoegen?", "Spelers", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    isAllPlayersAdded = true;
                }
            }
            


            MessageBox.Show($"Welkom, {gameStates[currentPlayer].PlayerName}! Je speelt met maximaal {gameStates[currentPlayer].Attempts} pogingen. Veel succes!", "Welkom", MessageBoxButton.OK);

            InitializeGame(); // Reset de speltoestand volledig
        }

        private void NewGameMenu_Click(object sender, RoutedEventArgs e)
        {

            InitializeGame(); // Reset de speltoestand volledig
            StartGame(); // Vraag een nieuwe spelernaam
            MessageBox.Show($"Nieuw spel gestart voor speler: {gameStates[currentPlayer].PlayerName}", "Nieuw Spel");
            StartCountDown();
        }


        private void StartCountDown()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void StopCountDown()
        {
            countDown = 10;
            timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            countDown--;
            timerCounter.Content = $"{countDown}";
            if (countDown == 0)
            {
                gameStates[currentPlayer].Attempts++;
                timer.Stop();
                if (gameStates[currentPlayer].Attempts >= gameStates[currentPlayer].MaxAttempts) // Gebruik de dynamische waarde
                {
                    GameOver();
                    return;
                }
                StopCountDown();
                UpdateTitle();
                StartCountDown();
            }
        }

        private void InitializeGame()
        {
            ClearAllChildrenRecursively(historyPanel);
            ResetAllColors();

         

            // Update labels
            scoreLabel.Content = $"Speler: {gameStates[currentPlayer].PlayerName}\nScore: {gameStates[currentPlayer].Score}\nPoging: {gameStates[currentPlayer].Attempts}";


            // Genereer een nieuwe geheime code
            Random number = new Random();
            secretCode = Enumerable.Range(0, 4)
                                   .Select(_ => colors[number.Next(colors.Length)])
                                   .ToArray();

            // Optioneel: toon de geheime code voor debugdoeleinden
            cheatCode.Text = string.Join(" ", secretCode);

            // Reset de timer
            if (timer.IsEnabled)
            {
                timer.Stop();
            }
            countDown = 10;
            timerCounter.Content = $"{countDown}";
            // Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() => { }));
            UpdateTitle();
        }

        private void ControlButton_Click(object sender, RoutedEventArgs e)
        {
            // Controleer of het spel al opnieuw is gestart
            if (gameStates[currentPlayer].Attempts >= gameStates[currentPlayer].MaxAttempts)
            {
                return; // Stop als het spel over is
            }

            List<Ellipse> ellipses = new List<Ellipse> { kleur1, kleur2, kleur3, kleur4 };
            string[] selectedColors = ellipses.Select(e => GetColorName(e.Fill)).ToArray();

            if (selectedColors.Any(color => color == "Transparent"))
            {
                MessageBox.Show("Selecteer vier kleuren!", "Foutief", MessageBoxButton.OK);
                return;
            }

            gameStates[currentPlayer].Attempts++;
            UpdateTitle();
            AddAttemptToHistory(selectedColors);
            UpdateScoreLabel(selectedColors);
            CheckGuess(selectedColors);

            if (gameStates[currentPlayer].Attempts >= gameStates[currentPlayer].MaxAttempts)
            {
                GameOver();
                return;
            }

            StopCountDown();
            StartCountDown();
        }

        private void CheckGuess(string[] selectedColors)
        {
            int correctPosition = 0;

            // Controleer correcte posities (kleur én positie)
            for (int i = 0; i < 4; i++)
            {
                if (selectedColors[i] == secretCode[i])
                {
                    correctPosition++;
                }
            }

            // Controleer of de speler gewonnen heeft
            if (correctPosition == 4)
            {
                HandleWin(); // Voer de winactie uit
            }
        }
        private void HandleWin()
        {
            timer.Stop();
            string highscoreEntry = $"{gameStates[currentPlayer].PlayerName} - {gameStates[currentPlayer].Attempts} pogingen - {gameStates[currentPlayer].Score}/100";
            AddHighscore(highscoreEntry);


            if (currentPlayer == gameStates.Count - 1)
            {
                highscoreMenu_Click(null, null);
            }
            // Vraag een nieuwe spelernaam en reset het spel volledig
            if(currentPlayer < gameStates.Count - 1){
                currentPlayer++;
            MessageBox.Show($"Code is gekraakt in {gameStates[currentPlayer - 1].Attempts}  pogingen\n" +
             $"Nu is speler {gameStates[currentPlayer].PlayerName} aan de beurt.", $"{gameStates[currentPlayer - 1].PlayerName}");
            }

            InitializeGame();
            // StartGame();
            StartCountDown();
        }

        void ClearAllChildrenRecursively(Panel panel)
        {
            foreach (var child in panel.Children.OfType<Panel>().ToList())
            {
                ClearAllChildrenRecursively(child); // Recursively clear nested containers
            }
            panel.Children.Clear(); // Clear the immediate children
        }

        private void AddAttemptToHistory(string[] selectedColors)
        {
            StackPanel attemptPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            for (int i = 0; i < selectedColors.Length; i++)
            {
                Ellipse colorBox = new Ellipse
                {
                    Width = 50,
                    Height = 50,
                    Fill = GetBrushFromColorName(selectedColors[i]),
                    StrokeThickness = 5,
                    Stroke = GetFeedbackBorder(selectedColors[i], i)
                };
                attemptPanel.Children.Add(colorBox);
            }

            historyPanel.Children.Add(attemptPanel);
        }

        

        private void UpdateScoreLabel(string[] selectedColors)
        {
            int scorePenalty = 0;

            for (int i = 0; i < selectedColors.Length; i++)
            {
                if (selectedColors[i] == secretCode[i])
                {
                    continue;
                }
                else if (secretCode.Contains(selectedColors[i]))
                {
                    scorePenalty += 1;
                }
                else
                {                
                    scorePenalty += 2;
                }
            }

            gameStates[currentPlayer].Score -= scorePenalty;
            if (gameStates[currentPlayer].Score < 0) gameStates[currentPlayer].Score = 0; 

            scoreLabel.Content = $"Speler: {gameStates[currentPlayer].PlayerName}\nScore: {gameStates[currentPlayer].Score}\nPoging: {gameStates[currentPlayer].Attempts}";
        }

        private Brush GetFeedbackBorder(string color, int index)
        {
            if (color == secretCode[index])
            {
                return Brushes.DarkRed; 
            }
            else if (secretCode.Contains(color))
            {
                return Brushes.Wheat; 
            }
            else
            {
                return Brushes.Transparent; 
            }
        }

        private void UpdateTitle()
        {
            this.Title = $"Poging {gameStates[currentPlayer].Attempts}/{gameStates[currentPlayer].MaxAttempts} - Speler: {gameStates[currentPlayer].PlayerName}";
        }

        private void ResetAllColors()
        {
            List<Ellipse> ellipses = new List<Ellipse> { kleur1, kleur2, kleur3, kleur4 };

            foreach (Ellipse ellipse in ellipses)
            {
                ellipse.Fill = Brushes.Red; // Zet de standaardkleur terug
                ellipse.Stroke = Brushes.Transparent; // Verwijder feedback-kleur
            }
        }

        private string GetColorName(Brush brush)
        {
            if (brush == Brushes.Red) return "Red";
            if (brush == Brushes.Yellow) return "Yellow";
            if (brush == Brushes.Orange) return "Orange";
            if (brush == Brushes.White) return "White";
            if (brush == Brushes.Green) return "Green";
            if (brush == Brushes.Blue) return "Blue";
            return "Transparent";
        }

        private void Toggledebug()
        {
            if (cheatCode.Visibility == Visibility.Hidden)
            {
                cheatCode.Visibility = Visibility.Visible;
            }
            else if (cheatCode.Visibility == Visibility.Visible)
            {
                cheatCode.Visibility = Visibility.Hidden;
            }
        }

        private void cheatCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.F12)
            {
                Toggledebug();
            }
        }

        private void kleur_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                ChangeEllipseColor(ellipse);
            }
        }

        private void ChangeEllipseColor(Ellipse ellipse)
        {
            SolidColorBrush currentBrush = ellipse.Fill as SolidColorBrush;
            int currentIndex = ellipseColor.IndexOf(currentBrush);

            int nextIndex = (currentIndex + 1) % ellipseColor.Count;
            ellipse.Fill = ellipseColor[nextIndex];
        }

        private void GameOver()
        {

            timer.Stop();
            string highscoreEntry = $"{gameStates[currentPlayer].PlayerName} - {gameStates[currentPlayer].Attempts} pogingen - {gameStates[currentPlayer].Score}/100";
            AddHighscore(highscoreEntry);


            // Reset het spel volledig
            
            if (currentPlayer == gameStates.Count - 1)
            {
                highscoreMenu_Click(null, null);
            }
            if(currentPlayer < gameStates.Count - 1){
                currentPlayer++;
            MessageBox.Show($"You failed! De correcte code was: {string.Join(", ", secretCode)}.\nNu is speler {gameStates[currentPlayer].PlayerName} aan de beurt.",
                            $"{gameStates[currentPlayer-1].PlayerName}",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
            }
            InitializeGame();
            // StartGame(); // Vraag een nieuwe naam
            StartCountDown();
        }

        private void AddHighscore(string highscoreEntry)
        {
            for (int i = 0; i < _highscores.Length; i++)
            {
                if (_highscores[i] == null)
                {
                    _highscores[i] = highscoreEntry;
                    return;
                }
            }

            // Als de lijst vol is, overschrijft de oudste score (FIFO)
            for (int i = 0; i < _highscores.Length - 1; i++)
            {
                _highscores[i] = _highscores[i + 1];
            }
            _highscores[_highscores.Length - 1] = highscoreEntry;
        }

        private string GetHighscores()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _highscores.Length; i++)
            {
                if (_highscores[i] != null)
                {
                    sb.AppendLine($"{i + 1}. {_highscores[i]}");
                }
            }

            return sb.ToString();
        }


        private Brush GetBrushFromColorName(string colorName)
        {
            return colorName switch
            {
                "Red" => Brushes.Red,
                "Yellow" => Brushes.Yellow,
                "Orange" => Brushes.Orange,
                "White" => Brushes.White,
                "Green" => Brushes.Green,
                "Blue" => Brushes.Blue,
                _ => Brushes.Transparent
            };
        }

        private void highscoreMenu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Highscores:\n" + GetHighscores(), "Highscores", MessageBoxButton.OK, MessageBoxImage.Information);

        }
       
    }
}
