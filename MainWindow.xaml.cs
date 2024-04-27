using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Digimon_World_enemy_randomizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Public variables
        //Randomizer identifiers
        public bool _DoWeRandomize = false;
        public bool _MakeCueFile = false;
        public bool _MakeSpoilerlog = false;
        public int _RandomizingEnemigos = 0;
        public int _RandomSeed = unchecked((int)Guid.NewGuid().GetHashCode());

        //file paths
        public string _LocationBaseFile = "";
        public string _LocationPatchFolder = AppContext.BaseDirectory + "data\\";
        public string _LocationResultFile = "";
        public bool _BaseFileSelected = false;
        public bool _PatchFolderSelected = true;
        public bool _ResultFileSelected = false;

        //data
        public List<string> _diginames = new List<string>();
        public List<string> _itemnames = new List<string>();

        public class Digimonbatallas
        {
            public byte _id;
            public short _HP;
            public short _MP;
            public short _HPmax;
            public short _MPmax;
            public short _ATQ;
            public short _DEF;
            public short _VEL;
            public short _INT;
            public short _Bits;
            public byte[] _Attacks;
            public byte[] _AttacksChance;

            public Digimonbatallas()
            {
                _id = 0;
                _HP = 0;
                _MP = 0;
                _HPmax = 0;
                _MPmax = 0;
                _ATQ = 0;
                _DEF = 0;
                _VEL = 0;
                _INT = 0;
                _Bits = 0;
                _Attacks = new byte[4];
                Array.Fill(_Attacks, (byte)0xFF);
                _AttacksChance = new byte[4];
                Array.Fill(_AttacksChance, (byte)0x00);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            //getting default data folder
            

            //initializing controls
            ComboItemEnemies.Items.Add("Don't randomize");
            ComboItemEnemies.Items.Add("Basic");
            ComboItemEnemies.Items.Add("Chaos");
            ComboItemEnemies.SelectedIndex = 0;
            TextSeed.Text = _RandomSeed.ToString();
            _DoWeRandomize = true;
            CheckDoSpoilerlog.IsEnabled = true;
            CheckDoSpoilerlog.IsChecked = true;
            ComboItemEnemies.IsEnabled = true;
            TextSeed.IsEnabled = true;
            TextSeed_Copy.IsEnabled = true;
        }

        //File selection
        private void ButtonChooseIso_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = ".iso image files (*.iso)|*.iso";
            if (openFileDialog.ShowDialog() == true)
            {
                _BaseFileSelected = true;
                _LocationBaseFile = openFileDialog.FileName;
                _ResultFileSelected = true;
                _LocationResultFile = openFileDialog.FileName + "-Randombattles.iso";
                TextResultLoc.Text = _LocationResultFile;
                TextIsoLoc.Text = _LocationBaseFile;
            }
            else
            {
                _BaseFileSelected = false;
            }
        }
        
        private void ButtonChooseResult_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = ".iso image files (*.iso)|*.iso";
            if (saveFileDialog.ShowDialog() == true)
            {
                _ResultFileSelected = true;
                _LocationResultFile = saveFileDialog.FileName;
                TextResultLoc.Text = _LocationResultFile;
            }
            else
            {
                _ResultFileSelected = false;
            }
        }

        //setting a seed manually
        private void ButtonChangeSeed_Click(object sender, RoutedEventArgs e)
        {
            int _parseOut;
            if (Int32.TryParse(TextSeed_Copy.Text, out _parseOut))
            {
                _RandomSeed = _parseOut;
                TextSeed.Text = _RandomSeed.ToString();
            }
            else
            {
                MessageBox.Show("Invalid seed format, please enter a number between:\n-2,147,483,647 y 2,147,483,647", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Want a CUE file?
        private void CheckCue_Checked(object sender, RoutedEventArgs e)
        {
            _MakeCueFile = true;
        }
        private void CheckCue_Unchecked(object sender, RoutedEventArgs e)
        {
            _MakeCueFile = false;
        }
        private void ComboItemEnemies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _RandomizingEnemigos = ComboItemEnemies.SelectedIndex;
        }
        private void CheckDoSpoilerlog_Checked(object sender, RoutedEventArgs e)
        {
            _MakeSpoilerlog = true;
        }
        private void CheckDoSpoilerlog_Unchecked(object sender, RoutedEventArgs e)
        {
            _MakeSpoilerlog = false;
        }
        //Parchear - patching
        private void ButtonPatchIt_Click(object sender, RoutedEventArgs e)
        {
            if (_BaseFileSelected && _PatchFolderSelected && _ResultFileSelected)
            {
                //init of the spoiler log
                string spoilerlogBody = "Digimon world battle randomizer with seed: " + _RandomSeed.ToString() + "\n";
                string _fileErrorString = "NOERROR"; //canary string
                string _xdeltaSTDout = "";
                string _xdeltaSTDerr = "";

                //check data before anything else
                if (!File.Exists(_LocationPatchFolder + "battledatanew.csv"))
                {
                    _fileErrorString = "File battledatanew.csv not found.";
                }
                if (!File.Exists(_LocationPatchFolder + "mapdata.csv"))
                {
                    _fileErrorString = "File mapdata.csv not found.";
                }
                if (!File.Exists(_LocationPatchFolder + "attacknumberdata.csv"))
                {
                    _fileErrorString = "File attacknumberdata.csv not found.";
                }
                if (!File.Exists(_LocationPatchFolder + "diginames.csv"))
                {
                    _fileErrorString = "File diginames.csv not found.";
                }
                try
                {
                    File.Copy(_LocationBaseFile, _LocationResultFile, false);
                }
                catch (IOException copyError)
                {
                    MessageBox.Show("Result ISO location file already exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    _fileErrorString=copyError.Message;
                }

                if (_DoWeRandomize && _fileErrorString == "NOERROR")
                {
                    _diginames.Clear();
                    _diginames.Add("Hiro"); //id=0

                    //now we fill the digimon name list
                    using (TextFieldParser csvParser = new TextFieldParser(_LocationPatchFolder + "diginames.csv"))
                    {
                        csvParser.CommentTokens = new string[] { "#" };
                        csvParser.SetDelimiters(new string[] { ";" });
                        // Skip the row with the column names
                        csvParser.ReadLine();
                        while (!csvParser.EndOfData)
                        {
                            string[] fields = csvParser.ReadFields();
                            _diginames.Add(fields[1]);
                        }
                    }
                    //set the seed
                    Random _RandoObject = new Random(_RandomSeed);

                    spoilerlogBody += "\n\n\nEnemies:\nConfig: " + ComboItemEnemies.SelectedItem + "\n";
                    if (_RandomizingEnemigos != 0)
                    {
                        spoilerlogBody += RandomizeEnemigos(_RandoObject);
                    }
                    if (_MakeSpoilerlog)
                    {
                        using (FileStream fs = new FileStream(_LocationResultFile + "_spoilerlog.txt", FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                sw.WriteLine(spoilerlogBody);
                            }
                        }
                    }
                    MessageBox.Show("Patching done!", "Patching done", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("No files selected!", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public string RandomizeEnemigos(Random Randyman)
        {

            int _RandomIndex = 0; //Variable to store our random number draws
            List<byte> _ListaDigiId = new List<byte>(); //List of digimons id
            List<byte> _MiniRandomList = new List<byte>(); //list used to draw random ids from, repopulated on every different map
            List<byte> _ListaDigiIdTechs = new List<byte>(); //List of possible attack animations, index equals digimon id
            List<int> _ListaDigiWeights = new List<int>(); //list of digimon weights, index equals digimon id
            Dictionary<string, int> _DicMaps = new Dictionary<string, int>(); //Dictionary with maps names as keys, and number of digimon as values
            Dictionary<int, Digimonbatallas> _DicOffsetsSpecies = new Dictionary<int, Digimonbatallas>(); //Dictionary for digimon species, the offset of the species is the key, and a class containing battle data the value
            string minispoilerlog = ""; //spoiler log string for this section
            string _previousmap = "none"; //control string
            int _MaxWeight = 140000; //Probably not the correct number, needs further testing to correctly narrow this value, howewer it is no greater than 140000 (For japanese version, US need testing to see if it can be greater)
            //Also this probably can be calculated, but i think that's beyond what i can figure out. 

            //we fill the digimon id list
            for (byte i = 3; i <= 113; i++)
            {
                switch (i)
                {
                    case 0: break; // skipping hiro
                    case 62: break; //skipping weregarurumon
                    default: _ListaDigiId.Add(i); break;
                }
            }

            //Now we fill the list of attacks and the weight list
            //_LocationPatchFolder is a string that contains the route to the patch files and the csv files to use
            using (TextFieldParser csvParser = new TextFieldParser(_LocationPatchFolder + "attacknumberdata.csv"))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { ";" });

                // Skip the row with the column names
                csvParser.ReadLine();
                while (!csvParser.EndOfData)
                {
                    string[] fields = csvParser.ReadFields();
                    //read attack data
                    _ListaDigiIdTechs.Add(Convert.ToByte(fields[1]));
                    //read weight data
                    _ListaDigiWeights.Add(Convert.ToInt32(fields[4]));
                }
            }
            //Now we load the map list
            using (TextFieldParser csvParser = new TextFieldParser(_LocationPatchFolder + "mapdata.csv"))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { ";" });

                // Skip the row with the column names
                csvParser.ReadLine();
                while (!csvParser.EndOfData)
                {
                    string[] fields = csvParser.ReadFields();
                    //add data to dictionary
                    _DicMaps.Add(fields[0], Convert.ToInt32(fields[1]));
                }
            }

            //now let's randomize
            using (FileStream fs = new FileStream(_LocationResultFile, FileMode.Open, FileAccess.Write))
            {
                using (BinaryWriter br = new BinaryWriter(fs, new ASCIIEncoding()))
                {
                    using (TextFieldParser csvParser = new TextFieldParser(_LocationPatchFolder + "battledatanew.csv"))
                    {
                        csvParser.CommentTokens = new string[] { "#" };
                        csvParser.SetDelimiters(new string[] { ";" });
                        int _offsetOfSpecies = 0;

                        // Skip the row with the column names
                        csvParser.ReadLine();

                        //and now we cycle through each row, one at a time.
                        while (!csvParser.EndOfData)
                        {
                            string[] fields = csvParser.ReadFields();
                            _offsetOfSpecies = Convert.ToInt32(fields[6], 16); //offset of species in maphead
                            //We check the room to see if it's new
                            if (fields[0] != _previousmap)
                            {
                                _MiniRandomList.Clear();//clear the random list for this map
                                _previousmap = fields[0];
                                // now we chek the number of species for this map
                                if (_DicMaps[_previousmap] > 1)
                                {
                                    //this map has more than one species at the same times (should be 2), we need to check weight limits to find a compatible pair of digimon.
                                    byte[] _digimongo = new byte[2];
                                    int _remainingWeight = 0;
                                    //We randomize the first digimon of the pair, and we get a random id for the id list.
                                    _RandomIndex = Randyman.Next(0, _ListaDigiId.Count);
                                    _digimongo[0] = _ListaDigiId[_RandomIndex];
                                    //We get the weight in bytes for the randomized digi, and subtract it from max weight to get our weight limit.
                                    _remainingWeight = _MaxWeight - _ListaDigiWeights[_digimongo[0]];
                                    //And now we list all digimons with a compatible weight
                                    List<byte> _tempIdlist = new List<byte>();
                                    for (int i = 0; i < _ListaDigiId.Count(); i++)
                                    {
                                        if (_ListaDigiWeights[_ListaDigiId[i]] < _remainingWeight)
                                        {
                                            _tempIdlist.Add(_ListaDigiId[i]);
                                        }
                                    }
                                    //To get a random one to add to the list
                                    _RandomIndex = Randyman.Next(0, _tempIdlist.Count);
                                    _digimongo[1] = _tempIdlist[_RandomIndex];

                                    //and we add the ids for this map to the random list
                                    _MiniRandomList.Add(_digimongo[0]);
                                    _MiniRandomList.Add(_digimongo[1]);
                                    _tempIdlist.Clear();
                                }
                                else
                                {
                                    //this map only has one species of the same type, so there's no need to check for limits, we copy all id to the random list
                                    _MiniRandomList.AddRange(_ListaDigiId);
                                }

                            }
                            //Have we randomized this species before?
                            //we have
                            if (_DicOffsetsSpecies.ContainsKey(_offsetOfSpecies))
                            {
                                //Chaos mode, all stats are randomized and all tech chances are random
                                //Also diferent digimon of the same species have different attacks too.
                                if (_RandomizingEnemigos > 1)
                                {
                                    //stats
                                    _DicOffsetsSpecies[_offsetOfSpecies]._HPmax = Convert.ToInt16(Randyman.Next(1, 9999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._MPmax = Convert.ToInt16(Randyman.Next(1, 9999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._HP = _DicOffsetsSpecies[_offsetOfSpecies]._HPmax;
                                    _DicOffsetsSpecies[_offsetOfSpecies]._MP = _DicOffsetsSpecies[_offsetOfSpecies]._MPmax;
                                    _DicOffsetsSpecies[_offsetOfSpecies]._ATQ = Convert.ToInt16(Randyman.Next(1, 999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._DEF = Convert.ToInt16(Randyman.Next(1, 999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._VEL = Convert.ToInt16(Randyman.Next(1, 999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._INT = Convert.ToInt16(Randyman.Next(1, 999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._Bits = Convert.ToInt16(Randyman.Next(1, 9999));

                                    //techs
                                    int _digimaxattacks = _ListaDigiIdTechs[_DicOffsetsSpecies[_offsetOfSpecies]._id];

                                    //we create and populate the attack list for this digimon
                                    List<byte> _ListTechs = new List<byte>();
                                    for (byte i = 1; i <= _digimaxattacks; i++)
                                    {
                                        byte addthisnbyte = 0x2D;
                                        addthisnbyte += i;
                                        _ListTechs.Add(addthisnbyte);
                                    }

                                    //First we tackle attack type, to do this we use the max number of techs and randomize it
                                    //If it has less than 4 techs the maxattacks number becomes the limit.
                                    for (int i = 0; i < Math.Min(4, _digimaxattacks); i++)
                                    {
                                        //We assign an attack
                                        _RandomIndex = Randyman.Next(0, _ListTechs.Count);
                                        _DicOffsetsSpecies[_offsetOfSpecies]._Attacks[i] = _ListTechs[_RandomIndex];
                                        //we take it off list
                                        _ListTechs.RemoveAt(_RandomIndex);

                                    }
                                    //Techs %
                                    //we calculate random numbers with a sum of 100
                                    //How many techs does this fella have?
                                    int _topechances = Math.Min(3, _digimaxattacks);
                                    int[] chances = new int[_topechances];
                                    //now we make a list of chances to not get duplicates (i know i'm being silly here, but i'm tired)
                                    List<int> listachance = new List<int>();
                                    for (int i = 1; i < 20; i++)
                                    {
                                        listachance.Add(i * 5);
                                    }
                                    //and we put all of them. This can be tweaked to allow repetition of values
                                    for (int i = 0; i < _topechances; i++)
                                    {
                                        _RandomIndex = Randyman.Next(0, listachance.Count);
                                        chances[i] = listachance[_RandomIndex];
                                        //Comment this if you want duplicate attack chances
                                        listachance.RemoveAt(_RandomIndex);
                                    }
                                    //now we sort all chances (that's what vanilla does)
                                    Array.Sort(chances);

                                    //now we set the chances to always add up to 100
                                    for (int i = 0; i <= _topechances; i++)
                                    {
                                        if (i == 0)
                                        {
                                            _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[i] = Convert.ToByte(chances[i]);
                                        }
                                        else
                                        {
                                            if (i == _topechances)
                                            {
                                                _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[i] = Convert.ToByte(100 - chances[i - 1]);
                                            }
                                            else
                                            {
                                                _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[i] = Convert.ToByte(chances[i] - chances[i - 1]);
                                                if (_DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[i] == 0)
                                                {
                                                    _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[i] += 5;
                                                    chances[i] += 5;
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                            //we haven't randomized this species before
                            else
                            {
                                //we add species key to dictionary
                                _DicOffsetsSpecies.Add(_offsetOfSpecies, new Digimonbatallas());
                                //Now populate with the correct info
                                _RandomIndex = Randyman.Next(0, _MiniRandomList.Count);
                                _DicOffsetsSpecies[_offsetOfSpecies]._id = _MiniRandomList[_RandomIndex];
                                _MiniRandomList.RemoveAt(_RandomIndex);

                                //techs
                                int _digiMaxAttacks = _ListaDigiIdTechs[_DicOffsetsSpecies[_offsetOfSpecies]._id];

                                //we create and populate the attack list for this digimon
                                List<byte> _ListaAtaques = new List<byte>();
                                for (byte i = 1; i <= _digiMaxAttacks; i++)
                                {
                                    byte addthisnbyte = 0x2D;
                                    addthisnbyte += i;
                                    _ListaAtaques.Add(addthisnbyte);
                                }

                                //First we tackle attack type, to do this we use the max number of techs and randomize it
                                //If we have less than 4 techs the maxattacks number becomes the limit.
                                for (int i = 0; i < Math.Min(4, _digiMaxAttacks); i++)
                                {
                                    //We assign an attack
                                    _RandomIndex = Randyman.Next(0, _ListaAtaques.Count);
                                    _DicOffsetsSpecies[_offsetOfSpecies]._Attacks[i] = _ListaAtaques[_RandomIndex];
                                    //we take it off list
                                    _ListaAtaques.RemoveAt(_RandomIndex);

                                }

                                //Chaos mode, all stats are randomized and all tech chances are random
                                if (_RandomizingEnemigos > 1)
                                {
                                    //stats
                                    _DicOffsetsSpecies[_offsetOfSpecies]._HPmax = Convert.ToInt16(Randyman.Next(1, 9999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._MPmax = Convert.ToInt16(Randyman.Next(1, 9999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._HP = _DicOffsetsSpecies[_offsetOfSpecies]._HPmax;
                                    _DicOffsetsSpecies[_offsetOfSpecies]._MP = _DicOffsetsSpecies[_offsetOfSpecies]._MPmax;
                                    _DicOffsetsSpecies[_offsetOfSpecies]._ATQ = Convert.ToInt16(Randyman.Next(1, 999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._DEF = Convert.ToInt16(Randyman.Next(1, 999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._VEL = Convert.ToInt16(Randyman.Next(1, 999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._INT = Convert.ToInt16(Randyman.Next(1, 999));
                                    _DicOffsetsSpecies[_offsetOfSpecies]._Bits = Convert.ToInt16(Randyman.Next(1, 9999));

                                    //Techs %
                                    //we calculate random numbers with a sum of 100
                                    //How many techs does this fella have?
                                    int _topechances = Math.Min(3, _digiMaxAttacks);
                                    int[] chances = new int[_topechances];
                                    //now we make a list of chances to not get duplicates (i know i'm being silly here, but i'm tired)
                                    List<int> listachance = new List<int>();
                                    for (int i = 1; i < 20; i++)
                                    {
                                        listachance.Add(i * 5);
                                    }
                                    //and we put all of them. This can be tweaked to allow repetition of values
                                    for (int i = 0; i < _topechances; i++)
                                    {
                                        _RandomIndex = Randyman.Next(0, listachance.Count);
                                        chances[i] = listachance[_RandomIndex];
                                        //Comment this if you want duplicate attack chances
                                        listachance.RemoveAt(_RandomIndex);
                                    }
                                    //now we sort all chances (that's what vanilla does)
                                    Array.Sort(chances);

                                    //now we set the chances to always add 100
                                    for (int i = 0; i <= _topechances; i++)
                                    {
                                        if (i == 0)
                                        {
                                            _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[i] = Convert.ToByte(chances[i]);
                                        }
                                        else
                                        {
                                            if (i == _topechances)
                                            {
                                                _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[i] = Convert.ToByte(100 - chances[i - 1]);
                                            }
                                            else
                                            {
                                                _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[i] = Convert.ToByte(chances[i] - chances[i - 1]);
                                            }
                                        }
                                    }

                                }

                            }

                            //write id in the 3 places -first species
                            br.BaseStream.Position = _offsetOfSpecies;
                            br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._id);
                            //offset of id in maphead
                            br.BaseStream.Position = Convert.ToInt32(fields[5], 16);
                            br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._id);
                            //offset for data in MAP file
                            br.BaseStream.Position = Convert.ToInt32(fields[4], 16);
                            br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._id);
                            br.BaseStream.Position += 21;

                            minispoilerlog += "\n" + fields[1] + " at " + fields[0] + " is now " + GetDigiName(_DicOffsetsSpecies[_offsetOfSpecies]._id);

                            //Writing stats
                            if (_RandomizingEnemigos > 1)
                            {
                                minispoilerlog += " con ";
                                //Now we do the checks and writes

                                //Current HP
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._HP);

                                //Current MP
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._MP);

                                //Max HP (we write this value to spoiler log)
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._HPmax);
                                minispoilerlog += "| HP: " + _DicOffsetsSpecies[_offsetOfSpecies]._HPmax;

                                //Max MP (we write this value to spoiler log)
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._MPmax);
                                minispoilerlog += "| MP: " + _DicOffsetsSpecies[_offsetOfSpecies]._MPmax;

                                //ATK
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._ATQ);
                                minispoilerlog += "| ATK: " + _DicOffsetsSpecies[_offsetOfSpecies]._ATQ;

                                //DEF
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._DEF);
                                minispoilerlog += "| DEF: " + _DicOffsetsSpecies[_offsetOfSpecies]._DEF;

                                //SPD
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._VEL);
                                minispoilerlog += "| SPD: " + _DicOffsetsSpecies[_offsetOfSpecies]._VEL;

                                //Brains
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._INT);
                                minispoilerlog += "| BRN: " + _DicOffsetsSpecies[_offsetOfSpecies]._INT;

                                //And the BITS reward for this guy
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._Bits);
                                minispoilerlog += "| Bits: " + _DicOffsetsSpecies[_offsetOfSpecies]._Bits;
                            }

                            minispoilerlog += "| Attacks: ";
                            //Writing attacks
                            br.BaseStream.Position = Convert.ToInt32(fields[4], 16) + 44; //set writer at correct offset
                            if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                            br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._Attacks[0]);
                            minispoilerlog += _DicOffsetsSpecies[_offsetOfSpecies]._Attacks[0] + " ";
                            br.BaseStream.Position += 1;
                            if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                            br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._Attacks[1]);
                            minispoilerlog += _DicOffsetsSpecies[_offsetOfSpecies]._Attacks[1] + " ";
                            br.BaseStream.Position += 1;
                            if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                            br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._Attacks[2]);
                            minispoilerlog += _DicOffsetsSpecies[_offsetOfSpecies]._Attacks[2] + " ";
                            br.BaseStream.Position += 1;
                            if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                            br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._Attacks[3]);
                            minispoilerlog += _DicOffsetsSpecies[_offsetOfSpecies]._Attacks[3] + " ";
                            br.BaseStream.Position += 1;

                            //writing attack chances
                            if (_RandomizingEnemigos > 1)
                            {
                                minispoilerlog += "| &Attacks: ";
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[0]);
                                minispoilerlog += _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[0] + " ";
                                br.BaseStream.Position += 1;
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[1]);
                                minispoilerlog += _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[1] + " ";
                                br.BaseStream.Position += 1;
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[2]);
                                minispoilerlog += _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[2] + " ";
                                br.BaseStream.Position += 1;
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                                br.Write(_DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[3]);
                                minispoilerlog += _DicOffsetsSpecies[_offsetOfSpecies]._AttacksChance[3];
                                br.BaseStream.Position += 1;
                                if (EvadeHeaderAndECC(br)) { br.BaseStream.Position += 304; }
                            }
                        }
                    }
                }
            }


            return minispoilerlog;
        }
        public bool EvadeHeaderAndECC(BinaryWriter _brtocheck)
        {
            long _positionToCheck = _brtocheck.BaseStream.Position;
            //2352 is mode 2 sector size in bytes
            long _offsetInSector = _positionToCheck % 2352;
            // 24 bytes for the header + 2048 datasize + last 280 bytes that is ERC/ECC = 2352 sector size
            //This returns true if we are in the special parts of this sector so we should advance 304 bytes.
            return !(24 < _offsetInSector && _offsetInSector < 2072);

        }
        private string GetDigiName(byte idtocheck)
        {
            string resultado = " ";
            if (idtocheck < 180)
            {
                resultado = _diginames[idtocheck];
            }
            return resultado;
        }
        private void ButtonChangeSeedRand_Click(object sender, RoutedEventArgs e)
        {
            //update the random seed
            _RandomSeed = unchecked((int)Guid.NewGuid().GetHashCode());
            TextSeed.Text = _RandomSeed.ToString();
        }
    }
}