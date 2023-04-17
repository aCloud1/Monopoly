using Monopolis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleMonopoly
{
    public enum ActionCode
    {
        NoAction = 1,
        HasNoOwner = 2,
        SkipTurns = 3,
        JumpToGO = 4,
        PlayLottery = 5,
        GO = 6,
        HasOwner = 7
    }

    public class Player
    {
        Board board;
        public string name;
        public int money = 250;
        public int skip_turns = 0;
        public int current_dice_roll;
        public LinkedListNode<Cell> current_cell;
        public bool in_jail;

        public int times_passed_go = 0;

        public Player(Board board, string name)
        {
            this.board = board;
            this.name = name;
            this.current_cell = board.cells.First;
            this.in_jail = false;
        }

        #region Getters/Setters
        public void setMoney(int value) { money = value; }
        public void addMoney(int value) { money += value; }
        public int getMoney() { return money; }
        public string getName() { return name; }
        public void setSkipTurns(int value) { skip_turns = value; }
        public int getSkipTurns() { return skip_turns; }
        public void setCurrentDiceRoll(int value) { current_dice_roll = value; }
        public int getCurrentDiceRoll() { return current_dice_roll; }

        public void incrementTimesPassedGo() { times_passed_go++; }
        public void resetTimesPassedGo() { times_passed_go = 0; }
        public int getTimesPassedGo() { return times_passed_go; }

        public LinkedListNode<Cell> getCurrentCellNode() { return current_cell; }
        public void setCurrentCellNode(LinkedListNode<Cell> node) { current_cell = node; }
        public Board getBoard() { return board; }
        public bool inJail() { return in_jail; }
        public void setInJail(bool value) { in_jail = value; }
        #endregion

        public void updateState()
        {
            if (getSkipTurns() > 0)
            {
                setInJail(true);
                Console.Write(getName() + " is sitting in jail.");
                Console.ReadLine();
                skip_turns--;
                return;
            }
            setInJail(false);
        }
        public void move()
        {
            int temp = getCurrentDiceRoll();
            while (temp != 0)
            {
                setCurrentCellNode(getCurrentCellNode().Next);
                if (getCurrentCellNode() == null)
                    setCurrentCellNode(getBoard().getCells().First);

                if(getCurrentCellNode().ValueRef.getActionCode() == ActionCode.GO)
                    incrementTimesPassedGo();

                temp--;
            }
        }

    }


    public class Cell
    {
        public string name;
        public int buying_price;
        public int rent_price;
        public Player owner;
        public ActionCode action_code;

        public Cell()
        {
            this.owner = null;
        }
        public Cell(string name, int buying_price, ActionCode ac = ActionCode.HasNoOwner)
        {
            this.owner = null;
            this.name = name;
            this.buying_price = buying_price;
            this.rent_price = buying_price / 2;
            this.action_code = ac;
        }

        public virtual void print()
        {
            Console.WriteLine("Name:\t\t" + getName());
            Console.WriteLine("Buying price:\t" + getBuyingPrice());
            Console.WriteLine("Rent price:\t" + getRentPrice());
            if (getOwner() != null)
                Console.WriteLine("Owner:\t\t" + getOwner().getName());
            else
                Console.WriteLine("Owner:\t\t-");
            Console.WriteLine("ActionCode:\t" + getActionCode());
        }

        public virtual void printInCreationMode()
        {
            Console.WriteLine("\t\t\t\tName:\t" + getName());
            Console.WriteLine("\t\t\t\tBuying price:\t" + getBuyingPrice());
            Console.WriteLine("\t\t\t\tRent price:\t" + getRentPrice());
            if (getOwner() != null)
                Console.WriteLine("\t\t\t\tOwner:\t" + getOwner().getName());
            else
                Console.WriteLine("\t\t\t\tOwner:\t\t-");
            Console.WriteLine("\t\t\t\tActionCode:\t" + getActionCode());
        }

        #region Getters/Setters
        public string getName() { return name; }
        public void setName(string value) { name = value; }
        public void setOwner(Player player) { owner = player; }
        public Player getOwner() { return owner; }
        public int getBuyingPrice() { return buying_price; }
        public void setBuyingPrice(int value) { buying_price = value; rent_price = buying_price / 2; }
        public int getRentPrice() { return rent_price; }

        public ActionCode getActionCode() { return this.action_code; }
        public void setActionCode(ActionCode ac) { this.action_code = ac; }
        #endregion

    }



    public class Board
    {
        public CircularLinkedList<Cell> cells;

        public Board()
        {
            cells = new CircularLinkedList<Cell>();
        }

        public void print()
        {
            LinkedListNode<Cell> node = getCells().First;
            if (node == null)
            {
                Console.WriteLine("\nBoard is empty.");
                return;
            }

            Console.WriteLine("\n+ ------------------------------ +");
            while (node != null)
            {
                node.ValueRef.print();
                Console.WriteLine();
                Console.WriteLine();

                node = node.Next;
            }
            Console.WriteLine("+ ------------------------------ +");

        }

        public void addCell(LinkedListNode<Cell> cell, int index)
        {
            if (index >= getCells().Count)
            {
                getCells().AddLast(cell);
                return;
            }
            else if (index <= 0)
            {
                getCells().AddFirst(cell);
                return;
            }
            LinkedListNode<Cell> index_cell = getCells().First;
            while (index != 1)
            {
                index_cell = index_cell.Next;
                index--;
            }
            getCells().AddAfter(index_cell, cell);

        }

        public void removeCell(int index)
        {
            if (index >= getCells().Count)
            {
                getCells().RemoveLast();
                return;
            }
            else if (index <= 0)
            {
                getCells().RemoveFirst();
                return;
            }
            LinkedListNode<Cell> index_cell = getCells().First;
            while (index != 0)
            {
                index_cell = index_cell.Next;
                index--;
            }
            getCells().Remove(index_cell);

        }

        public void clear() { getCells().Clear(); }

        public bool save(string file_name)
        {
            LinkedListNode<Cell> node = getCells().First;
            using StreamWriter file = new StreamWriter(file_name + ".json");
            while (node != null)
            {
                string json;
                try
                {
                    json = JsonConvert.SerializeObject(node.ValueRef);
                    file.WriteLineAsync(json);
                    node = node.Next;
                }
                catch (Newtonsoft.Json.JsonSerializationException)
                {
                    file.Close();
                    return false;
                }
            }
            file.Close();
            return true;
        }

        public bool load(string file_name)
        {
            if (!File.Exists(file_name))
                return false;

            // check if file is not empty
            if (new FileInfo(file_name).Length == 0)
                return false;

            foreach (string line in File.ReadLines(file_name))
            {
                try
                {
                    Cell cell = JsonConvert.DeserializeObject<Cell>(line);
                    getCells().AddLast(cell);
                }
                catch (Newtonsoft.Json.JsonReaderException)
                {
                    return false;
                }
            }
            return true;
        }
        
        #region Getters/Setters
        public CircularLinkedList<Cell> getCells() { return cells; }
        public bool isEmpty() { return getCells().Count == 0; }
        #endregion
    
    }

}
