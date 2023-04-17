using ConsoleMonopoly;

namespace Monopolis
{
    public class Menu
    {
        Game game;
        public bool in_menu_main = false;
        public bool in_menu_game = false;
        public bool in_board_creation = false;


        public Menu(Game game)
        {
            this.game = game;
        }

        public void openMainMenu()
        {
            Console.WriteLine("\n-----Main menu-----");
            Console.WriteLine("1. New game");
            Console.WriteLine("2. Exit");
            Console.Write("Enter selection: ");
        }

        public void openGameMenu()
        {
            Console.WriteLine("\n\t-----New game-----");
            Console.WriteLine("\t1. Select board");
            Console.WriteLine("\t2. Create board");
            Console.WriteLine("\t3. Back");
            Console.Write("\tEnter selection: ");
        }

        public void openBoardSelection()
        {
            // scan for available board files
            getGame().getJsonReader().list();
            int i = 0;
            Console.WriteLine("\n\tAvailable boards:");
            foreach (var l in getGame().getJsonReader().getFilePaths())
            {
                Console.WriteLine("\t" + (i+1) + ". " + Path.GetFileNameWithoutExtension(l));
                i++;
            }

        }

        public void listBoardCreationActions()
        {
            Console.Write("\n\t\t-----Board Creation-----\n" +
                "\t\t1. Insert cell\n" +
                "\t\t2. Delete cell\n" +
                "\t\t3. Display board\n" +
                "\t\t4. Clear board\n" +
                "\t\t5. Save board\n" +
                "\t\t6. Load board\n" +
                "\t\t7. Exit\n" +
                "\t\tEnter selection: ");
        }

        public static bool promptUser(string message, ConsoleKey prompt_activation_key, ConsoleKey confirmation_key)
        {
            if (Console.ReadKey().Key != prompt_activation_key)
                return true;

            Console.Write(message);
            if (Console.ReadKey(false).Key != confirmation_key)
            {
                Console.WriteLine();
                return true;
            }
            return false;
        }

        public static bool promptUser(string message, ConsoleKey confirmation_key)
        {
            Console.Write(message);
            if (Console.ReadKey(false).Key != confirmation_key)
            {
                Console.WriteLine();
                return true;
            }
            return false;
        }

        #region Getters/Setters
        public bool inMenuMain() { return in_menu_main; }
        public bool inMenuGame() { return in_menu_game; }
        public void setInMenuGame(bool value) { in_menu_game = value; }
        public bool inBoardCreation() { return in_board_creation;  }
        public Game getGame() { return game; }
        #endregion
    
    }



    public class Game
    {
        public int turn;
        public Board board;
        public Random rng;
        public Menu menu;
        public ConsoleMonopoly.JsonReader json_reader;
        public List<Player> players;
        public bool running;
        public bool session_running;
        public int money_goal = 1000;
        public int go_reward = 200;
        public int RNG_MIN_VAL = -100;
        public int RNG_MAX_VAL = 100;
        public string[] player_names = { "Jonas", "Petras", "Ieva", "Onutė" };


        public Game()
        {
            menu = new Menu(this);
            json_reader = new ConsoleMonopoly.JsonReader();
            running = true;
            session_running = false;
        }

        public int rollDice() { return getRNG().Next(6) + 1; }

        private void init(int player_count)
        {
            board = new Board();
            rng = new Random();

            players = new List<Player>();
            for (int i = 0; i < player_count; i++)
                players.Add(new Player(board, player_names[i]));

            session_running = true;
        }

        public void update()
        {
            if(!Menu.promptUser("Are you sure you want to quit (y/n)? ", ConsoleKey.Escape, ConsoleKey.Y))
            {
                setIsInGame(false);
                return;
            }

            foreach (var p in players)
            {
                p.updateState();
                if (p.inJail())
                {
                    Console.WriteLine(p.getName() + " skips this turn.");
                    Console.ReadLine();
                    continue;
                }

                Console.Write(p.getName() + ", throw dice: ");
                Console.ReadLine();
                p.setCurrentDiceRoll(rollDice());
                p.move();
                Console.WriteLine(p.getCurrentDiceRoll() + "! You are at " + p.getCurrentCellNode().ValueRef.getName());
                rewardForPassingGo(p);
                activateCell(p);
            }


        }

        public void draw()
        {
            Console.Clear();

            getBoard().print();
            foreach (var p in getPlayers())
            {
                Console.Write(p.getName() + " is at: \t" + p.getCurrentCellNode().ValueRef.getName() + ",\tMoney = $" + p.getMoney());
                if (p.inJail())
                    Console.Write(". Skips " + p.getSkipTurns() + " turn(/-s).");
                Console.Write("\n");
            }
            Console.WriteLine();
            Console.WriteLine();

        }

        private void activateCell(Player player)
        {
            Cell ccell = player.getCurrentCellNode().ValueRef;
            switch (ccell.getActionCode())
            {
                #region No Action
                case ActionCode.NoAction:
                    break;
                #endregion

                #region Has No Owner
                case ActionCode.HasNoOwner:
                    if (ccell.getOwner() == null)
                    {
                        Console.Write(ccell.getName() + " does not have an owner. You have $" + player.getMoney() + ". Costs $" + ccell.getBuyingPrice() + ". Buy it (y/n)? ");
                        if (Console.ReadKey(false).Key == ConsoleKey.Y)
                        {
                            if (player.getMoney() >= ccell.getBuyingPrice())
                            {
                                ccell.setOwner(player);
                                ccell.setActionCode(ActionCode.HasOwner);
                                player.addMoney(-ccell.getBuyingPrice());
                                Console.WriteLine("\n" + player.getName() + " now owns " + player.getCurrentCellNode().ValueRef.getName() + ".");
                            }
                            else
                            {
                                Console.WriteLine("\nInsufficient funds.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("\nYou skipped.");
                        }
                    }
                    break;
                #endregion

                #region Has Owner
                case ActionCode.HasOwner:
                    if (ccell.getOwner() != null)
                    {
                        Console.WriteLine(ccell.getName() + " is owned by " + ccell.getOwner().getName());
                        if (player == ccell.getOwner())
                        {
                            Console.WriteLine("Welcome home!");
                        }
                        else
                        {
                            Console.WriteLine(player.getName() + " pays " + ccell.getOwner().getName() + " $" + ccell.getRentPrice() + ".");
                            player.addMoney(-ccell.getRentPrice());
                            ccell.getOwner().addMoney(ccell.getRentPrice());
                        }
                    }
                    break;
                #endregion

                #region Jump To GO
                case ActionCode.JumpToGO:
                    LinkedListNode<Cell> node = player.getCurrentCellNode();
                    while(true)
                    {
                        if (node.ValueRef.getActionCode() == ActionCode.GO)
                        {
                            player.setCurrentCellNode(node);
                            break;
                        }
                        node = node.Next;
                        if (node == null)
                            node = getBoard().getCells().First;
                    }
                    Console.WriteLine(player.getName() + ", you jump to " + node.ValueRef.getName() + ".");
                    player.incrementTimesPassedGo();
                    rewardForPassingGo(player);
                    break;
                #endregion

                #region Skip Turns
                case ActionCode.SkipTurns:
                    player.setInJail(true);
                    player.setSkipTurns(3);
                    break;
                #endregion

                #region Play Lottery
                case ActionCode.PlayLottery:
                    Console.Write("Welcome, " + player.getName() + "! ");
                    if (!Menu.promptUser("Do you want to play the lottery (y/n)? ", ConsoleKey.Y))
                    {
                        int r = getRandomInt(RNG_MIN_VAL, RNG_MAX_VAL);
                        if (r > 0)
                            Console.WriteLine("\nYou are lucky! You've won " + r + "!");
                        else if (r < 0)
                            Console.WriteLine("\nBad luck! You pay " + r + ".");
                        else if (r == 0)
                            Console.WriteLine("\nYou win nothing.");
                        player.addMoney(r);
                    }
                    else
                    {
                        Console.WriteLine("Maybe next time?");
                    }
                    
                    break;
                #endregion
            }

            Console.ReadLine();

        }

        public void rewardForPassingGo(Player p)
        {
            if (p.getTimesPassedGo() != 0)
            {
                int reward = p.getTimesPassedGo() * getGoReward();
                Console.Write(p.getName() + " passed GO. Reward: " + reward + "$.");        
                p.addMoney(reward);
                p.resetTimesPassedGo();
                Console.ReadLine();
            }
        }

        private Player checkForWinners()
        {
            Player winner = null;
            int max = getGoal();
            foreach (var p in getPlayers())
            {
                if(p.getMoney() >= max)
                {
                    max = p.getMoney();
                    winner = p;
                }
            }

            if (winner != null)
                setIsInGame(false);

            return winner;
        }

        public void announceWinner(Player winner)
        {
            Console.WriteLine("Congratulations " + winner.getName() + "! You win!");
        }

        public bool selectAndLoadBoard()
        {
            int board_number;
            getMenu().openBoardSelection();
            while(true)
            {
                Console.Write("\tSelect board: ");
                board_number = Input.askForIntegerInput();
                if(board_number > 0 && board_number <= getJsonReader().getFilePaths().Count)
                    break;

                Console.WriteLine("\tTry again.");
            }

            bool result = loadBoard(getJsonReader().getFilePaths().ElementAt(board_number - 1));
            if (!result)
            {
                Console.WriteLine("\tLoading failed.");
                return false;
            }
            return true;
        }

        public void setUpPlayers()
        {
            int count;
            while(true)
            {
                Console.Write("\n\tNumber of players [2-4]: ");
                count = Input.askForIntegerInput();
                if (count >= 2 && count <= 4)
                    break;
                Console.WriteLine("\tNumber out of range.");
            }
            init(count);
        }

        public void setUpGoal()
        {
            int count;
            while (true)
            {
                Console.Write("\n\tEnter amount of money to win the game: ");
                count = Input.askForIntegerInput();
                if (count > 0 && count != -1)
                {
                    setGoal(count);
                    break;
                }
                Console.Write("\tInvalid input.");
            }
        }

        public void play()
        {
            Player winner = null;

            while (isRunning())
            {
                getMenu().openMainMenu();
                int selection = Input.askForIntegerInput();
                if (selection == 1) // new game
                {
                    getMenu().setInMenuGame(true);
                    while (getMenu().inMenuGame())
                    {
                        getMenu().openGameMenu();
                        selection = Input.askForIntegerInput();
                        if (selection == 1)  // select board and play
                        {
                            setUpPlayers(); // select player count
                            setUpGoal();    // enter goal

                            // select board
                            if (!selectAndLoadBoard())
                                continue;

                            // play game
                            while (isInGame())
                            {
                                draw();
                                update();
                                winner = checkForWinners();
                            }

                            if (winner != null)
                                announceWinner(winner);

                        }
                        else if (selection == 2)  // board creation
                        {
                            createBoard();
                            Console.WriteLine();
                        }
                        else if (selection == 3)  // go back
                        {
                            Console.WriteLine();
                            getMenu().setInMenuGame(false);
                            break;
                        }
                    }
                }
                else if (selection == 2)    // exit game
                {
                    Console.WriteLine("Exiting game...");
                    Console.WriteLine();
                    setRunning(false);
                }
                else
                {
                    Console.WriteLine("Try again.");
                    Console.WriteLine();
                }
            }

        }

        public bool saveBoard(string file_name)
        {
            return getBoard().save(file_name);
        }

        public bool loadBoard(string file_name)
        {
            bool result = getBoard().load(file_name);
            if (result && getPlayers() != null)
                foreach (Player p in getPlayers())
                    p.setCurrentCellNode(getBoard().getCells().First);
            return result;
        }

        public void createBoard()
        {
            bool creating_board = true;
            bool unsaved_changes = false;
            board = new Board();
            getBoard().print();

            while (creating_board)
            {
                getMenu().listBoardCreationActions();
                int selection = Input.askForIntegerInput();
                switch (selection)
                {
                    #region Insert Cell
                    case 1:
                        bool creating_cell = true;
                        while (creating_cell)
                        {
                            Cell new_cell = new Cell();
                            Console.WriteLine("\n\t\t\t-----Inserting Cell-----");

                            // print action codes
                            Console.WriteLine("\t\t\tAction codes:");
                            int i = 0;
                            for (; i < 6; i++)
                                Console.WriteLine("\t\t\t" + (i + 1) + ". " + (ActionCode)Enum.GetValues(typeof(ActionCode)).GetValue(i));

                            // select action code
                            Console.Write("\t\t\t" + (i + 1) + ". Cancel\n\t\t\tSelect action code: ");
                            int c = Input.askForIntegerInput();
                            if (c == i + 1)
                            {
                                Console.WriteLine("\t\tInsertion canceled.");
                                break;
                            }
                            if (c <= 0 || c > i + 1)
                            {
                                Console.WriteLine("\t\tTry again.");
                                continue;
                            }
                            Console.WriteLine("\t\t\t* Selected " + (ActionCode)Enum.GetValues(typeof(ActionCode)).GetValue(c - 1));
                            new_cell.setActionCode((ActionCode)Enum.GetValues(typeof(ActionCode)).GetValue(c - 1));

                            // get name
                            Console.Write("\n\t\t\tEnter name: ");
                            string name = Input.getString();
                            Console.WriteLine("\t\t\t* Entered " + name + "\n");
                            new_cell.setName(name);

                            // get buying price
                            int price;
                            do
                            {
                                Console.Write("\t\t\tEnter buying price: ");
                                price = Input.askForIntegerInput();
                            } while (price == -1);
                            Console.WriteLine("\t\t\t* Entered " + price + "\n");
                            new_cell.setBuyingPrice(price);

                            // get index
                            int index;
                            do
                            {
                                Console.Write("\t\t\tWhere to insert? (index): ");
                                index = Input.askForIntegerInput();
                            } while (index == -1);
                            getBoard().addCell(new LinkedListNode<Cell>(new_cell), index);

                            Console.WriteLine("\nBoard after inserting new element:");
                            getBoard().print();
                            Console.WriteLine();
                            creating_cell = false;
                            unsaved_changes = true;
                        }
                        break;
                    #endregion

                    #region Delete Cell
                    case 2:
                        if (getBoard().isEmpty())
                        {
                            Console.WriteLine("\n\t\tBoard is empty.");
                            break;
                        }
                        Console.WriteLine("\n\t\t\t-----Deleting Cell-----");
                        Console.WriteLine("\t\t\tDelete by:\n\t\t\t1. Name\n\t\t\t2. Index");
                        int s;
                        while (true)
                        {
                            Console.Write("\t\t\tSelection: ");
                            s = Input.askForIntegerInput();
                            if (s == 1)
                            {
                                Console.Write("\t\t\tEnter name of the cell: ");
                                string name = Input.getString();

                                // find cells with name, prompt user to delete each
                                LinkedListNode<Cell> node = getBoard().getCells().First;
                                int occurence = 0;
                                int cell_number = 1;
                                while (node != null)
                                {
                                    if (node.ValueRef.getName() == name)
                                    {
                                        // found it
                                        occurence++;
                                        Console.WriteLine("\t\t\t______________________________");
                                        Console.WriteLine("\t\t\tOccurence: " + occurence + "\tCell number: " + cell_number + "\n");
                                        node.ValueRef.printInCreationMode();
                                        if (!Menu.promptUser("\t\t\tDelete this cell (y/n)? ", ConsoleKey.Y))
                                        {
                                            LinkedListNode<Cell> temp = node;
                                            node = node.Next;
                                            getBoard().getCells().Remove(temp);
                                            cell_number++;
                                            Console.WriteLine();
                                            continue;
                                        }
                                        else
                                        {
                                            node = node.Next;
                                            cell_number++;
                                            Console.WriteLine();
                                        }
                                    }
                                    else
                                    {
                                        node = node.Next;
                                        cell_number++;
                                    }
                                }
                                if (occurence == 0)
                                    Console.WriteLine("Cell with name \"" + name + "\" does not exist.");
                                break;
                            }
                            else if (s == 2)
                            {
                                Console.Write("\t\t\tEnter index of the cell: ");
                                int index = Input.askForIntegerInput();
                                getBoard().removeCell(index);
                                Console.WriteLine("Deleting cell at index " + index);
                                break;
                            }
                            Console.WriteLine("\t\t\tTry again.");
                        }

                        getBoard().print();
                        Console.WriteLine();
                        unsaved_changes = true;
                        break;
                    #endregion

                    #region Display Board
                    case 3:
                        getBoard().print();
                        Console.WriteLine("\n");
                        break;
                    #endregion

                    #region Clear Board
                    case 4:
                        getBoard().clear();
                        Console.WriteLine("Board cleared.\n");
                        unsaved_changes = true;
                        break;
                    #endregion

                    #region Save Board
                    case 5:
                        Console.Write("\t\t\tEnter board name: ");
                        string file_name = Input.getString();
                        Console.WriteLine("Saving...\n");
                        if (saveBoard(file_name))
                        {
                            unsaved_changes = false;
                            Console.WriteLine("Save successful.");
                        }
                        else Console.WriteLine("Could not save board.");
                        break;
                    #endregion

                    #region Load Board
                    case 6:
                        if (unsaved_changes)
                        {
                            if (!Menu.promptUser("\t\t\tThere are unsaved changes. Are you sure you want to continue (y/n)?", ConsoleKey.Y))
                            {
                                getBoard().clear();
                                if (!selectAndLoadBoard())
                                    break;
                                getBoard().print();
                                unsaved_changes = false;
                            }
                        }
                        else
                        {
                            getBoard().clear();
                            if (!selectAndLoadBoard())
                                break;
                            getBoard().print();
                            unsaved_changes = false;
                        }
                        break;
                    #endregion

                    #region Exit
                    case 7:
                        if (unsaved_changes)
                        {
                            if (!Menu.promptUser("\t\t\tThere are unsaved changes. Are you sure (y/n)?", ConsoleKey.Y))
                                creating_board = false;
                        }
                        else
                            creating_board = false;
                        break;
                    #endregion

                    #region Default
                    default:
                        Console.WriteLine("Try again.\n");
                        break;
                        #endregion
                }
            }

        }


        #region Getters/Setters
        public int getGoReward() { return go_reward; }
        public Board getBoard() { return board; }
        public List<Player> getPlayers() { return players; }
        public ConsoleMonopoly.JsonReader getJsonReader() { return json_reader; }
        public Random getRNG() { return rng; }
        public int getRandomInt(int min_val, int max_val) { return (int)(getRNG().NextDouble() * (max_val - min_val) + min_val); }
        public Menu getMenu() { return menu; }
        public bool isRunning() { return running; }
        public void setRunning(bool value) { running = false; }
        public bool isInGame() { return session_running; }
        public void setIsInGame(bool value) { session_running = value; }
        public void setGoal(int value) { money_goal = value; }
        public int getGoal() { return money_goal; }
        #endregion


        static void Main(String[] args)
        {
            Game g = new Game();
            g.play();
        }

    }
}