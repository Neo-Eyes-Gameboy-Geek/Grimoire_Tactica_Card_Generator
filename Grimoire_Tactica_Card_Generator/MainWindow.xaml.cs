using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace Grimoire_Tactica_Card_Generator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Important to set the styling of text
            Style = (Style)FindResource(typeof(Window));
        }

        //When the window is loaded little actually needs to be done, just populate the list of sets that are in the sets folder
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //The set list needs to be populated on startup 
            Static_Methods_Container.Populate_Set_List(this);
        }

        private void CMB_Set_Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //When the set drop down gets changed, the card list needs to be updated to represent the new set selected
            //Start by changing the selected Card in the combo box to an empty box
            CMB_Card_Selector.SelectedIndex = -1;
            //Empty The Text Boxes of any content 
            Static_Methods_Container.Clear_Text_Boxes(this);
            //And Empty the card list 
            CMB_Card_Selector.Items.Clear();
            //get the set from the selected item if they exist
            if (CMB_Set_Selector.SelectedItem != null && (Set_Info)((ComboBoxItem)CMB_Set_Selector.SelectedItem).Tag != null)
            {
                ComboBoxItem New_Selection = (ComboBoxItem)CMB_Set_Selector.SelectedItem;
                List<Card> cards = ((Set_Info)(New_Selection.Tag)).cards;
                //and repopulate the card drop down with those cards
                Static_Methods_Container.Populate_Card_List(this, cards);
            }
        }

        private void CMB_Card_Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //First we need to clear the textboxes 
            Static_Methods_Container.Clear_Text_Boxes(this);
            //and then if the new selection has a functional Card in the tag update the textboxes with this new data
            if (CMB_Card_Selector.SelectedItem != null && (Card)((ComboBoxItem)CMB_Card_Selector.SelectedItem).Tag != null)
            {
                //Fill the textboxes 
                Card card = (Card)((ComboBoxItem)CMB_Card_Selector.SelectedItem).Tag;
                Static_Methods_Container.Populate_Text_Boxes(this, card);
            }
        }

        //Making a new set basically just entails creating a new directory from a name provided from a dialog box
        //then add this new directory to the list of sets
        private void BTN_Add_Set_Click(object sender, RoutedEventArgs e)
        {
            //First off we need a dialog to know what to call the new set, and hence the directory to put it in
            InputDialog input = new InputDialog("New Set Name?");
            if (input.ShowDialog() == true)
            {
                //Assuming they give a proper responce and didnt cancel we can use the value given as the 
                //name of the new directory for the set
                //TODO make it so Set_Directory is available everywhere in the window
                const string Set_Directory = @".\Sets";
                string New_Set_Directory = Set_Directory + $@"\{input.Answer()}";
                //This new folder will be a subdirectory of the Set_Directory
                //We need to make sure we only make the directory if it does not already exist
                if (!Directory.Exists(New_Set_Directory))
                {
                    Directory.CreateDirectory(New_Set_Directory);
                    //Now we have the directory path all setup, we can use that to 
                    //make the combobox item that will hold all this information
                    CMB_Set_Selector.Items.Add(Static_Methods_Container.Generate_Set_Item(New_Set_Directory));
                }
                else
                {
                    //Show an error saying that a folder by that name already exists
                    MessageBox.Show("Error: A folder by that name already exists! No new folder has been created.", "Folder Already Exists!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BTN_Delete_Set_Click(object sender, RoutedEventArgs e)
        {
            //This will target and delete the directory of the selected item assuming a prompt is passed
            InputDialog input = new InputDialog("Are You Sure You Want To Delete The Selected Set?\nThis Action Cannot Be Undone!\n(y/n)");
            if (input.ShowDialog() == true)
            {
                if (input.Answer().ToLower() == "y" && CMB_Set_Selector.SelectedItem != null)
                {
                    //Can't delete what doesnt exist obviously
                    Static_Methods_Container.Clear_Text_Boxes(this);
                    CMB_Card_Selector.SelectedIndex = -1;
                    CMB_Card_Selector.Items.Clear();
                    //We need to get the directory from the currently selected set
                    Set_Info info = (Set_Info)((ComboBoxItem)CMB_Set_Selector.SelectedItem).Tag;
                    //And use the directory to delete the whole directory
                    //the true at the end indicates recursive delete so as to delete contents as well
                    Directory.Delete(info.dir, true);
                    //Lastly we Remove the currently selected item from the list since it holds nothing anymore
                    CMB_Set_Selector.Items.RemoveAt(CMB_Set_Selector.SelectedIndex);
                }
            }
        }

        private void BTN_Delete_Card_Click(object sender, RoutedEventArgs e)
        {
            //This does basically the same thing to a card as Delete_Set does to a whole set
            //We need to ensure this is something the user wishes to do 
            InputDialog input = new InputDialog("Are You Sure You Want To Delete This Card?\nThis Action Cannot Be Undone!\n(y/n)");
            if (input.ShowDialog() == true)
            {
                if (input.Answer().ToLower() == "y" && CMB_Card_Selector.SelectedItem != null)
                {
                    Static_Methods_Container.Clear_Text_Boxes(this);
                    //First we will need the card object out of the selected item
                    Card target = (Card)((ComboBoxItem)CMB_Card_Selector.SelectedItem).Tag;
                    //This card contains the fileinfo we can use to delete the file 
                    FileInfo info = target.Text_File;
                    if (info.Exists)
                    {                        
                        info.Delete();
                    }
                    //We then remove the selected card from the selected sets list
                    //First we need to extract the list of cards from the selected set to remove from
                    List<Card> current_set = ((Set_Info)((ComboBoxItem)CMB_Set_Selector.SelectedItem).Tag).cards;
                    //Remove the target card from this list
                    current_set.Remove(target);
                    //Lastly Delete the entry on the combo box since it references nothing
                    CMB_Card_Selector.Items.RemoveAt(CMB_Card_Selector.SelectedIndex);
                }
            }
        }

        //This Will Add A New Card To the Currently Selected Set
        private void BTN_Add_Card_Click(object sender, RoutedEventArgs e)
        {            
            //First off we need the name of file to write this to
            InputDialog input = new InputDialog("Name Of File To Save New Card To!");
            if (input.ShowDialog() == true)
            {
                //Also need to make sure an actual set is selected to add the card to
                if (CMB_Set_Selector.SelectedItem != null)
                {
                    //The final name of the file 
                    string New_File = input.Answer();
                    //sanitise the string so as not to crash the program
                    string regexSearch = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars());
                    Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
                    //this removes invalid characters from the file name
                    New_File = r.Replace(New_File, "");
                    //Now we need to get the directory to put it in from the selected set
                    string directory = ((Set_Info)((ComboBoxItem)CMB_Set_Selector.SelectedItem).Tag).dir;
                    //Now make this described file and make a file info of it
                    FileInfo file = new FileInfo(directory + $@"\{New_File}.txt");
                    //Only create the file if there isn't an existing file by that name in the directory
                    if (!File.Exists(file.FullName))
                    {
                        //Actually create the file , making sure to close the filestream it leaves behind
                        file.Create().Close();
                        //the set we will be adding cards to
                        List<Card> set = ((Set_Info)((ComboBoxItem)CMB_Set_Selector.SelectedItem).Tag).cards;
                        //now make a card from the file, creating the file in the process 
                        //we also need to give the card the index , which will be whatever the lists length is
                        //since indexing starts at 0
                        int index = set.Count;
                        Card c = Card.Generate_Card_From_File(file,index);
                        //Also give the card a generic name and title so it doesnt show up
                        //as a blank entry on the drop down
                        c.Name = "New";
                        c.Title = "Card";
                        //We now add this card to the list of the current set                        
                        set.Add(c);
                        //And then append the card to the combobox
                        CMB_Card_Selector.Items.Add(Static_Methods_Container.Generate_Card_Item(c));
                    }
                    else
                    {
                        //If the file already exists dont make a new one, you will just end up fighting over access
                        //Show an error saying that a file by that name already exists
                        MessageBox.Show("Error: A file by that name already exists! No new file has been created.", "Folder Already Exists!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void BTN_Save_Set_Click(object sender, RoutedEventArgs e)
        {
            //This saves the entire set of cards currently selected into their respective text files
            //First we need to make sure the current card is as up do date as can be
            Static_Methods_Container.Update_Current_Card(this);
            //Now it's simply a matter of extracting the list of currently selected cards from the set_info of the selected set
            //Obviously assuming its selected and it has an existant set_info
            if ((ComboBoxItem)(CMB_Set_Selector.SelectedItem) != null && (Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag) != null)
            {
                //Now we know it's safe to do so extract this set info and pass it as a directory to save all the cards
                Set_Info info = (Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag);
                Functions.Save_Set(info.cards);
            }
        }

        private void BTN_Save_Card_Click(object sender, RoutedEventArgs e)
        {
            //This will save the individual card being worked on
            //First we update the current card so we save the up to date information
            Static_Methods_Container.Update_Current_Card(this);
            //Now ensuring we have a valid card to do this to save it to its associated file
            if ((ComboBoxItem)CMB_Card_Selector.SelectedItem != null && (Card)((ComboBoxItem)CMB_Card_Selector.SelectedItem).Tag != null)
            {
                Card c = (Card)((ComboBoxItem)CMB_Card_Selector.SelectedItem).Tag;
                Functions.Save_Card(c);
            }
        }

        //This will add a new textbox to the list of abilities, with the correct formatting
        private void BTN_Add_Ability_Click(object sender, RoutedEventArgs e)
        {
            //First we need a new listviewitem and textbox to add to the list
            ListViewItem item = new ListViewItem();
            TextBox box = new TextBox();
            //now fill the box with the correct placeholder
            box.Text = Card.ABILITY_FORMAT;
            //fill the list item with the textbox and add it to the list
            item.Content = box;
            LIV_Abilities.Items.Add(item);
            //now update the current card to inform it that it has a new ability
            Static_Methods_Container.Update_Current_Card(this);
        }

        //This will remove the currently selected ability (if any) and then update the current card to remind it that it has one less ability
        private void BTN_Delete_Ability_Click(object sender, RoutedEventArgs e)
        {
            //This will delete the currently selected listview item if any and removes it 
            if (LIV_Abilities.SelectedIndex >= 0 && LIV_Abilities.SelectedItems != null)
            {
                LIV_Abilities.Items.RemoveAt(LIV_Abilities.SelectedIndex);
            }
            //And update the card just for good measure
            Static_Methods_Container.Update_Current_Card(this);
        }

        //This produces a cropped image of the currently selected card and makes it the source for the preview image item
        //on the screen
        private void BTN_Preview_Card_Click(object sender, RoutedEventArgs e)
        {
            //This should only work if there is a valid card selected
            if((ComboBoxItem)CMB_Card_Selector.SelectedItem != null && (Card)((ComboBoxItem)CMB_Card_Selector.SelectedItem).Tag != null)
            {
                //Make sure the cards are all as up to date as possible
                Static_Methods_Container.Update_Current_Card(this);
                //and that changes wont be lost
                Set_Info info = (Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag);
                Functions.Save_Set(info.cards);
                //First step is to dispose of the existing bitmap source if any in the preview window
                IMG_Card_Preview.Source = null;                
                //then from the selected card generate a cropped image from the card selected 
                Card c = (Card)((ComboBoxItem)CMB_Card_Selector.SelectedItem).Tag;
                BitmapImage i;
                //now make a new image source with standard boxes
                using (Bitmap b = Card.Generate_Cropped_Image(c, "PREVIEW", @".\Overlays\Overlay.png"))
                {                    
                    using(MemoryStream mem = new MemoryStream())
                    {
                        //save the bitmap to the stream for use of the image
                        b.Save(mem, System.Drawing.Imaging.ImageFormat.Bmp);
                        mem.Position = 0;
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = mem;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();
                        i = image;
                    }                    
                }
                IMG_Card_Preview.Source = i;                               
            }
        }

        //This will take the currently selected set, update the current card and generate a set of full bleed sized cards
        //To a path specified by the user
        private void BTN_Export_Bleed_Set_Click(object sender, RoutedEventArgs e)
        {
            //TODO double check this method
            //first of all we need to only do this is there is a set selected and its tag is not null
            if((ComboBoxItem)CMB_Set_Selector.SelectedItem != null && (Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag) != null)
            {
                //Make sure the cards are all as up to date as possible
                Static_Methods_Container.Update_Current_Card(this);
                //and that changes wont be lost
                Set_Info info = (Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag);
                Functions.Save_Set(info.cards);
                string set_code = "";
                //First of all we need the code used to identify the set in the corner
                InputDialog input = new InputDialog("Please Enter Set Identifier.\nIn form xxxx where 'x' is a letter or number!");
                if (input.ShowDialog() == true)
                {
                    //We obviously only continue if they dont cancel the dialog
                    set_code = input.Answer();
                    //we also need to pick the overlay boxes we are using 
                    //we need the list of cards out of selected item
                    List<Card> set = ((Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag)).cards;
                    //we also need to make a bleed folder for the images if they dont exist
                    string Bleed_Directory = ((Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag)).dir + @"\Bleed";
                    Directory.CreateDirectory(Bleed_Directory);
                    //we also need an overlay to use, user can pick but just in case have a default selected
                    string overlay = Card.DEFAULT_OVERLAY;
                    //but give the user a choice 
                    OpenFileDialog dialog = new OpenFileDialog();
                    //set a file filter
                    dialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*";
                    //and make sure it opens wherever the program is for access to the overlays folder
                    dialog.InitialDirectory = $@"{Directory.GetCurrentDirectory()}\Overlays";
                    if (dialog.ShowDialog() == true)
                    {
                        //if they selected one set it as the new overlay
                        overlay = dialog.FileName;
                    }
                    //now for sake of efficiency we will generate and save the cards in a parrallel fashion
                    //The limit of 12 concurrent tasks is mostly to avoid out of memory issues since in a stress
                    //situation each iteration can use up to a gig of memory each so not bounding how many
                    //could cause some problems, this still will on lower end systems but shouldn't be an issue
                    //if you arent throwing massive images at it constantly
                    Parallel.ForEach(set,new ParallelOptions {MaxDegreeOfParallelism = 12 }, c =>
                    {
                        using (Bitmap b = Card.Generate_Bleed_Image(c, set_code, overlay))
                        {
                            //The encoder needs some set up to function properly
                            string s = string.Format("{0:00000}", c.Index);
                            string filepath = $@"{Bleed_Directory}\{set_code}-{s}-Bleed.png";                                                                                  
                            b.Save(filepath, ImageFormat.Png);
                        }
                    });                    
                }
                
            }
        }

        //As above but the images will be cropped to the normal size, not inclusive of the bleed
        //Useful for things like tabletop simulator
        private void BTN_Export_Cropped_Set_Click(object sender, RoutedEventArgs e)
        {
            //first of all we need to only do this is there is a set selected and its tag is not null
            if ((ComboBoxItem)CMB_Set_Selector.SelectedItem != null && (Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag) != null)
            {
                //Make sure the cards are all as up to date as possible
                Static_Methods_Container.Update_Current_Card(this);
                //and that changes wont be lost
                Set_Info info = (Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag);
                Functions.Save_Set(info.cards);
                string set_code = "";
                //First of all we need the code used to identify the set in the corner
                InputDialog input = new InputDialog("Please Enter Set Identifier.\nIn form xxxx where 'x' is a letter or number!");
                if (input.ShowDialog() == true)
                {
                    //We obviously only continue if they dont cancel the dialog
                    set_code = input.Answer();
                    //we need the list of cards out of selected item
                    List<Card> set = ((Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag)).cards;
                    //we also need to make a bleed folder for the images if they dont exist
                    string Cropped_Directory = ((Set_Info)(((ComboBoxItem)(CMB_Set_Selector.SelectedItem)).Tag)).dir + @"\Cropped";
                    Directory.CreateDirectory(Cropped_Directory);
                    //now for sake of efficiency we will generate and save the cards in a parrallel fashion 
                    //The limit of 8 concurrent tasks is mostly to avoid out of memory issues since in a stress
                    //situation each iteration can use up to a gig of memory each so not bounding how many
                    //could cause some problems, this still will on lower end systems but shouldn't be an issue
                    //if you arent throwing massive images at it constantly
                    //We also need to know the overlay to use , although set a default, just in case
                    string overlay = Card.DEFAULT_OVERLAY;
                    //but give the user a choice 
                    OpenFileDialog dialog = new OpenFileDialog();
                    //set a file filter
                    dialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*";
                    dialog.InitialDirectory = $@"{Directory.GetCurrentDirectory()}\Overlays";
                    if (dialog.ShowDialog() == true)
                    {
                        //if they selected one set it as the new overlay
                        overlay = dialog.FileName;
                    }
                    Parallel.ForEach(set, new ParallelOptions { MaxDegreeOfParallelism = 12 }, c =>
                    {
                        using (Bitmap b = Card.Generate_Cropped_Image(c, set_code, overlay))
                        {
                            //The encoder needs some set up to function properly
                            string s = string.Format("{0:00000}", c.Index);
                            string filepath = $@"{Cropped_Directory}\{set_code}-{s}-Cropped.png";                                                                                  
                            b.Save(filepath, ImageFormat.Png);
                        }
                    });
                }

            }
        }

        //This will open up a file viewer to get the relative path to the image that will be used for the artists signature
        private void BTN_Artist_Signature_Path_Finder_Click(object sender, RoutedEventArgs e)
        {
            //First we need an open file dialog to select the file from
            OpenFileDialog dialog = new OpenFileDialog();
            //Note, I'd set a relative file path but the file dialog wont accept a relative file path, so wherever it opens 
            //is fine
            //And set it to filter just the image file formats we want
            //in this case .pngs or .jpgs, or failing that, basically anything although results may vary
            dialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*";
            //now assuming the user doesn't cancel the file selection
            if (dialog.ShowDialog() == true)
            {
                //set the artists signature path as the path to the selected file
                string Absolute_Path = dialog.FileName;
                TBX_Artist_Signature_Path.Text = Absolute_Path;
            }
            //and now all that is done update the current card
            Static_Methods_Container.Update_Current_Card(this);
        }

        //Same as above but for the path to the artwork 
        private void BTN_Art_Path_Finder_Click(object sender, RoutedEventArgs e)
        {
            //First we need an open file dialog to select the file from
            OpenFileDialog dialog = new OpenFileDialog();
            //Note, I'd set a relative file path but the file dialog wont accept a relative file path, so wherever it opens 
            //is fine
            //And set it to filter just the image file formats we want
            //in this case .pngs or .jpgs, or failing that, basically anything although results may vary
            dialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*";
            //now assuming the user doesn't cancel the file selection
            if (dialog.ShowDialog() == true)
            {
                //set the artists signature path as the path to the selected file
                string Absolute_Path = dialog.FileName;
                TBX_Artwork_Path.Text = Absolute_Path;
            }
            //and now all that is done update the current card
            Static_Methods_Container.Update_Current_Card(this);
        }
    }

    //A small container class to hold both a directory info and a list of cards for a given set
    public class Set_Info
    {
        public string dir { get; set; }
        public List<Card> cards { get; set; }
    }

    //This Class will hold the static methods the window will use, to keep the window class less cluttered
    public static class Static_Methods_Container
    {
        //This Method will populate the set dropdown of a given MainWindow based on the child directories of the Set directory 
        public static void Populate_Set_List(MainWindow w)
        {
            //The Default Directory That Sets Should Be Stored In.
            //This in theory will be in the same folder as the exe, hence the relative path
            const string Set_Directory = @".\Sets";
            //It is imperative that the Sets folder exists in order for this to work
            if (Directory.Exists(Set_Directory))
            {
                //Assuming the directory exists, each set will have its own subdirectory, what we need is to get all of those
                String[] Sets_List = Directory.GetDirectories(Set_Directory);
                //And each of these directories will get its own object in the Set combobox 
                foreach (string set in Sets_List)
                {                    
                    w.CMB_Set_Selector.Items.Add(Generate_Set_Item(set));
                }                
            }
            else
            {
                //If the directory doesnt exist create it.
                //Obviouslly a newly created directory wont have any sets in it so that ends the startup process
                Directory.CreateDirectory(Set_Directory);
            }
        }

        //This method produces a comboboxitem for a set given a path 
        public static ComboBoxItem Generate_Set_Item(string set)
        {
            //So first we need to make the new combo box item
            ComboBoxItem item = new ComboBoxItem();
            //the tag will store the whole path of the folder for easier access later
            //plus a full list of the card objects, to reduce file I/O
            Set_Info new_set = new Set_Info();
            new_set.dir = set;
            new_set.cards = new List<Card>();
            //This does however mean that all the work for making these card items happens here
            //instead of when the card list gets populated                    
            if (Directory.Exists(set))
            {
                FileInfo[] files = (new DirectoryInfo(set)).GetFiles(@"*.txt");
                //now for each file that matches this search criteria we need to make a new card and add it to the list
                //remembering to zero index them
                int index = 0;
                foreach (FileInfo file in files)
                {
                    new_set.cards.Add(Card.Generate_Card_From_File(file, index));
                    index++;
                }
            }
            item.Tag = new_set;
            item.Content = new DirectoryInfo(set).Name;
            return item;
        }

        //This method meanwhile populates the list of cards of a given MainWindow based on a list of cards supplied
        public static void Populate_Card_List(MainWindow w, List<Card> c)
        {
            //Needs to iterate once for every card in the list
            foreach (Card card in c)
            {                
                //Each Card Gets Its Own ComboBoxItem
                w.CMB_Card_Selector.Items.Add(Generate_Card_Item(card));
            }
        }

        //This Creates A Combo Box Item For a Card given to it 
        public static ComboBoxItem Generate_Card_Item(Card c)
        {
            //Make a new combo box item
            ComboBoxItem item = new ComboBoxItem();
            //The title of which is the Name and Title concatenated
            item.Content = $"{c.Name} {c.Title}";
            //whilst the tag is the card object itself
            item.Tag = c;
            return item;
        }

        //Populates a windows text boxes with data from a provided card
        public static void Populate_Text_Boxes(MainWindow w, Card card)
        {
            w.TBX_Card_Name.Text = card.Name;
            w.TBX_Card_Title.Text = card.Title;
            w.TBX_Card_Cost.Text = card.Cost;
            w.TBX_Card_HP.Text = card.HP;
            w.TBX_Card_ATK.Text = card.ATK;
            w.TBX_Card_DEF.Text = card.DEF;
            w.TBX_Card_Skills.Text = card.Skills;
            w.TBX_Card_Keywords.Text = card.Keywords;
            //There is a variable number of abilities so a for each is needed
            foreach (string s in card.Abilities)
            {
                //Each Ability will sit in a textbox on a listviewitem on the combobox 
                ListViewItem item = new ListViewItem();
                TextBox box = new TextBox();
                box.Text = s;
                item.Content = box;
                w.LIV_Abilities.Items.Add(item);
            }
            //And finally set the last couple text boxes 
            w.TBX_Flavour_Text.Text = card.Flavour_Text;
            w.TBX_Artist_Signature_Path.Text = card.Signature;
            w.TBX_Artwork_Path.Text = card.Artwork;
            w.TBX_Rarity.Text = card.Rarity;
        }

        //Clear the data entry fields on a provided main window
        public static void Clear_Text_Boxes(MainWindow w)
        {
            //just make all the textboxes empty strings 
            w.TBX_Card_Name.Text = "";
            w.TBX_Card_Title.Text = "";
            w.TBX_Card_Cost.Text = "";
            w.TBX_Card_HP.Text = "";
            w.TBX_Card_ATK.Text = "";
            w.TBX_Card_DEF.Text = "";
            w.TBX_Card_Skills.Text = "";
            w.TBX_Card_Keywords.Text = "";
            //And empty out the stackpanel of abilities 
            w.LIV_Abilities.Items.Clear();
            //and finish clearing the text boxes
            w.TBX_Flavour_Text.Text = "";
            w.TBX_Artist_Signature_Path.Text = "";
            w.TBX_Artwork_Path.Text = "";
            w.TBX_Rarity.Text = "";
        }

        //Update the Card object in the list from the textboxes on the screen
        public static void Update_Current_Card(MainWindow w)
        {
            //First up we are going to need to make sure there is a valid card item to be
            //updating with this delicious information
            if((ComboBoxItem)w.CMB_Card_Selector.SelectedItem != null && (Card)((ComboBoxItem)w.CMB_Card_Selector.SelectedItem).Tag != null)
            {
                //Now safe in our knowledge that this card exists for us to update we need a reference to it to pump with data
                Card c = (Card)((ComboBoxItem)w.CMB_Card_Selector.SelectedItem).Tag;
                //Now its just a matter of copying the appropriate textboxes texts into the right data fields
                c.Name = w.TBX_Card_Name.Text;
                c.Title = w.TBX_Card_Title.Text;
                c.Cost = w.TBX_Card_Cost.Text;
                c.HP = w.TBX_Card_HP.Text;
                c.ATK = w.TBX_Card_ATK.Text;
                c.DEF = w.TBX_Card_DEF.Text;
                c.Skills = w.TBX_Card_Skills.Text;
                c.Keywords = w.TBX_Card_Keywords.Text;
                //Abilities as always require a little more work
                //The old contents of the list need clearing out first
                c.Abilities.Clear();
                //then each ListViewItem in the stack panel will have a line to add to Abilities
                foreach(ListViewItem item in w.LIV_Abilities.Items)
                {
                    //we need the textbox out of the item 
                    TextBox box = (TextBox)item.Content;
                    //It's text is what is added to the 
                    c.Abilities.Add(box.Text);
                }
                //Then It's just the last couple data fields
                c.Flavour_Text = w.TBX_Flavour_Text.Text;
                c.Signature = w.TBX_Artist_Signature_Path.Text;
                c.Artwork = w.TBX_Artwork_Path.Text;
                c.Rarity = w.TBX_Rarity.Text;
                //Oh and for sake of completeness update the name of the current card
                ((ComboBoxItem)w.CMB_Card_Selector.SelectedItem).Content = $"{c.Name} {c.Title}";
            }
        }
    }
}
