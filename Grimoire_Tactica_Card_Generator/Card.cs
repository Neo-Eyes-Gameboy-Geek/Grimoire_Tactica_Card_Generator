﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grimoire_Tactica_Card_Generator
{
    public class Card
    {
        //Constants related to cards as a whole
        //the string used to identify that the abilities has ended
        public const string END_ABILITIES_STRING = "<<End Abilities>>";
        //placeholder string for the correct format of an ability in the file should be in
        public const string ABILITY_FORMAT = "Title|Effect";
        //This lists the type of cards, used to draw the card type icon on the left for quick reference
        public static readonly string[] CARD_TYPES = { "Hero", "Character", "Equipment", "Location", "Event", "Ability", "Token" };
        //Constants for the size of a bleed and cropped card
        public const int BLEED_X = 825;
        public const int BLEED_Y = 1125;
        public const int CROPPED_X = 750;
        public const int CROPPED_Y = 1050;
        public const int ABILITY_WIDTH = 554;
        public const int ABILITY_HEIGHT = 260;
        //and the bottom right corner where the artists signature finishes so it can be right justified
        public const int Signature_X = 740;
        public const int Signature_Y = 1043;
        //Constants for the font families used on the cards
        public const string NAME_FONT = "Courier New";
        public const string NUMBER_FONT = "Courier New";
        public const string ABILITY_FONT = "Courier New";
        public const string FLAVOUR_FONT = "Courier New";
        public const string CODE_FONT = "Courier New";
        //And a constant for the deafault overlay
        public const string DEFAULT_OVERLAY = @".\Overlays\Overlay.png";
        //Data Fields, All Strings for ease of use
        public string Name { get; set; }
        public string Title { get; set; }
        public string Cost { get; set; }
        public string Skills { get; set; }
        public string Keywords { get; set; }
        public string HP { get; set; }
        public string ATK { get; set; }
        public string DEF { get; set; }
        public List<string> Abilities { get; set; }
        public string Flavour_Text { get; set; }
        //These last 2 are file paths, to a signature from the artist and the artwork itself
        public string Signature { get; set; }
        public string Artwork { get; set; }
        //and one for rarity, to affect the set list colour
        public string Rarity { get; set; }
        //Lastly this is just the path to the text file this will all be stored in, this should in theory not change
        public FileInfo Text_File { get; set; }
        //An integer representing what order the card exists in in the set
        //this is set at run time and so doesnt need to be stored in or read from a text file
        public int Index { get; set; }

        //Constructors omitted since only a no-arg constructor is used and an argumented constructor would have a mile long paramter list

        //Methods for the class

        //Static Methods for the class
        public static Card Generate_Card_From_File(FileInfo file, int i)
        {            
            //just for safetys sake ensure the file mentioned actually exists
            Card c = new Card();
            //And initiate an ability list so it exists even if empty
            c.Abilities = new List<string>();
            if (file.Exists)
            {
                //need a streamreader to read the file provided to us
                using(StreamReader s = new StreamReader(file.FullName))
                {
                    //Just as a buff to robustness need to make sure there is actually a line to read before writing anything else
                    //the string should just be empty
                    if (!s.EndOfStream)
                    {
                        c.Name = s.ReadLine().Trim();
                    }
                    
                    if (!s.EndOfStream)
                    {
                        c.Title = s.ReadLine().Trim();
                    }
                    
                    if (!s.EndOfStream)
                    {
                        c.Cost = s.ReadLine().Trim();
                    }
                    
                    if (!s.EndOfStream)
                    {
                        c.Skills = s.ReadLine().Trim();
                    }
                    
                    if (!s.EndOfStream)
                    {
                        c.Keywords = s.ReadLine().Trim();
                    }
                    
                    if (!s.EndOfStream)
                    {
                        c.HP = s.ReadLine().Trim();
                    }
                    
                    if (!s.EndOfStream)
                    {
                        c.ATK = s.ReadLine().Trim();
                    }

                    if (!s.EndOfStream)
                    {
                        c.DEF = s.ReadLine().Trim();
                    }

                    bool More_Abilities = true;
                    //Abilities are of a mutable number, so this step needs to keep going until we reach either EoF or the stop string
                    if (!s.EndOfStream)
                    {
                        do
                        {
                            string a = s.ReadLine().Trim();
                            if (a != END_ABILITIES_STRING)
                            {
                                c.Abilities.Add(a);
                            }
                            else
                            {
                                More_Abilities = false;
                            }
                        }
                        while (More_Abilities && !s.EndOfStream);
                    }

                    //now it's just filling in the couple remaining data fields
                    if (!s.EndOfStream)
                    {
                        c.Flavour_Text = s.ReadLine().Trim();
                    }

                    if (!s.EndOfStream)
                    {
                        c.Signature = s.ReadLine().Trim();
                    }

                    if (!s.EndOfStream)
                    {
                        c.Artwork = s.ReadLine().Trim();
                    }

                    if (!s.EndOfStream)
                    {
                        c.Rarity = s.ReadLine().Trim();
                    }

                    //Lastly tie the fileinfo to the card for easier retreival later
                    c.Text_File = file;
                }
            }
            //Irrespective the index is set to whatever i was passed as
            c.Index = i;
            //an empty card is still a card so no else statement is required
            return c;
        }

        //Produces a full bleed card image from the card data provided
        public static Bitmap Generate_Bleed_Image(Card c, string set, string boxes)
        {
            //this is the bitmap that will actually be returned
            Bitmap canvas;
            //the functioning of this method relies entirely on the initial piece
            //of artwork existing for use so only continue if it does
            if (File.Exists(c.Artwork))
            {
                //if it does we can start cropping and scaling it down to size
                using(Bitmap a = new Bitmap(c.Artwork))
                {
                    //now because the scale and crop return new bitmaps of their own
                    //the crop although not the scale will need their own using statements
                    using(Bitmap b = Functions.Crop_Image(a, Card.BLEED_X, Card.BLEED_Y))
                    {
                        //crop is done first cos it puts the image in the right aspect ratio
                        canvas = Functions.Scale_Image(b, Card.BLEED_X, Card.BLEED_Y);
                        //Any magic numbers included are based off the textboxes drawn in the template
                        //psd file used to make the card layout in the first place, so changes there will
                        //need changes here but no more than having a mountain of constants in the class
                        //Next step is to overlay the textboxes onto the freshly resized artwork
                        //these textboxes are compiled with the project as an image but are picked
                        //from at run time
                        using(Bitmap overlay = new Bitmap(boxes))
                        {
                            //draw the textboxes into place on the card
                            canvas = Functions.Overlay_Image(canvas, overlay, new Rectangle(80, 80, 665, 965));
                        }
                        //Next up we need to draw the icons in place, there can be more than 1 so we need to split the string up
                        //and draw every one that occurs
                        string[] Icons = c.Skills.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach(string icon in Icons)
                        {
                            //for each of them we call drawicon
                            //trimming the string to avoid potential non detections from
                            //extra white space
                            Functions.Draw_Icon(canvas, icon.Trim());
                        }
                        //Now we write the text in place, most of it is plain text with only
                        //the abilities, which need extra processing any way, that are rich text
                        //May as well start from top left through to bottom right                        
                        //Rectangle for the main name of the card to appear in
                        Rectangle Name_Rectangle = new Rectangle(205, 82, 504, 60);
                        canvas = Functions.Write_Text(canvas, c.Name, Name_Rectangle, true, Card.NAME_FONT, true, false, Brushes.Black);
                        //And the card title
                        Rectangle Title_Rectangle = new Rectangle(205, 147, 504, 30);
                        //the title is written in the same font as the name just not as a bold
                        canvas = Functions.Write_Text(canvas, c.Title, Title_Rectangle, true, Card.NAME_FONT, true, false, Brushes.Black);
                        //keywords need a little processing to assemble the string we want to draw since it will be stored
                        //slightly more compactly that being drawn
                        //keywords are stored all together but compacted by a comma, we need that to become a space
                        string keywords = "";
                        string[] keys = c.Keywords.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach(string key in keys)
                        {
                            //now for each of these keys we add it trimmed of white space to the keywords string
                            keywords += $"[{key.Trim()}] ";
                            //and if the keyword happens to be a card type keyword draw its icon
                            if (Card.CARD_TYPES.Contains(key.Trim()))
                            {
                                Functions.Draw_Icon(canvas, key.Trim());
                            }
                        }
                        //and trim keywords to avoid drawing empty space
                        keywords.Trim();
                        //keywords is then what we write to the card, with the same font as numbers but not bolded
                        Rectangle Keyword_Rectangle = new Rectangle(85, 671, 665, 40);
                        //and draw the keywords in 
                        canvas = Functions.Write_Text(canvas, keywords, Keyword_Rectangle, true, Card.NUMBER_FONT, true, false, Brushes.Black);
                        //The rectangle the cost of the card appears in, do this after keywords so the type icon is drawn under the cost text
                        Rectangle Cost_Rectangle = new Rectangle(85, 85, 90, 90);
                        canvas = Functions.Write_Text(canvas, c.Cost, Cost_Rectangle, true, Card.NUMBER_FONT, true, false, Brushes.Black);
                        //HP, ATK and DEF all follow basically the same pattern, just the box they are in changes
                        Rectangle HP_Rectangle = new Rectangle(85, 722, 90, 90);
                        canvas = Functions.Write_Text(canvas, c.HP, HP_Rectangle, true, Card.NUMBER_FONT, true, false, Brushes.Black);
                        Rectangle ATK_Rectangle = new Rectangle(85, 823, 90, 90);
                        canvas = Functions.Write_Text(canvas, c.ATK, ATK_Rectangle, true, Card.NUMBER_FONT, true, false, Brushes.Black);
                        Rectangle DEF_Rectangle = new Rectangle(85, 924, 90, 90);
                        canvas = Functions.Write_Text(canvas, c.DEF, DEF_Rectangle, true, Card.NUMBER_FONT, true, false, Brushes.Black);
                        //Abilities due to their varied number in a fixed box require a little bit it processing
                        //the box is 554 pixels wide and 260 pixels tall and all the abilities need to fit into that
                        //Abilities also are split into a bolded title and a non bold body so those need to be drawn seperate
                        //No point wasting space on abilities that wont be drawn since they are invalid so we first need
                        //a count of abilities in the desired format as well as store these valid abilities for easier access
                        int Valid_Abilities = 0;
                        List<string[]> Valid_Ability_List = new List<string[]>();
                        foreach(string ability in c.Abilities)
                        {
                            //split the ability into its two component parts
                            string[] split_ability = ability.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                            //then if it has the right number of fields add it to the list of valid abilities
                            if(split_ability.Length >= Card.ABILITY_FORMAT.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Length)
                            {
                                Valid_Abilities++;
                                Valid_Ability_List.Add(split_ability);
                            }
                        }
                        //since some cards wont have abilities we need to make sure Valid_Abilities is always at least 1
                        Valid_Abilities = Math.Max(1, Valid_Abilities);
                        //this number ends up forming the basis for all the maths needed to draw all the abilities properly
                        int Ability_Box_Height = Card.ABILITY_HEIGHT / Valid_Abilities;
                        //the title is only allowed to take up 1/5th this boxes total height so the lions share goes to the body text
                        int Ability_Title_Height = Ability_Box_Height / 4;
                        int Ability_Body_Height = Ability_Box_Height - Ability_Title_Height;
                        //Since all of these need to be drawn to rectangles we need values to track where the top of the rectangles go
                        int Rect_X = 186;
                        int Rect_Y = 722;
                        //now draw each of these abilities 
                        foreach(string[] abil in Valid_Ability_List)
                        {
                            //the first entry should be the Title of the Ability which is written on bold at the top
                            //this doesnt need wrapping so cramming it onto one line is fine
                            canvas = Functions.Write_Text(canvas, abil[0], new Rectangle(Rect_X, Rect_Y, Card.ABILITY_WIDTH, Ability_Title_Height), false, Card.ABILITY_FONT, true, false, Brushes.Black);
                            //now advance the Rect_Y posistion by the height of what was just written
                            Rect_Y += Ability_Title_Height;
                            //The body of the ability is stored in index 1 of the array and it does need to be written with text wrapping and with some words bolded
                            //usually keywords
                            canvas = Functions.Write_Rich_Text(canvas, abil[1], new Rectangle(Rect_X, Rect_Y, Card.ABILITY_WIDTH, Ability_Body_Height), Card.ABILITY_FONT, false, false, Brushes.Black);
                            //Advance the cursor further so it is in posistion for the next ability
                            Rect_Y += Ability_Body_Height;
                        }
                        //The flavourtext still needs to be written as wrapped rich text but since there will only ever be 1 body of it, drawing it is far simpler
                        Rectangle Flavour_Rectangle = new Rectangle(Rect_X, 984, Card.ABILITY_WIDTH, 30);
                        //Flavour Text has its own font and its always written itallic in a lighter font and shade to distinguish it
                        //in this case a nice gray
                        using(SolidBrush brush = new SolidBrush(ColorTranslator.FromHtml("#363636")))
                        {
                            canvas = Functions.Write_Rich_Text(canvas, c.Flavour_Text, Flavour_Rectangle, Card.FLAVOUR_FONT, false, true, brush);
                        }                        
                        //A small amount of work is needed to generate the string the identifies the card in the set
                        //since its a formatted version of the index + the string for the set ID passed as a parameter
                        //the index needs to be padded to be 
                        string Set_Code = $"{set}-{string.Format("{0:00000}", c.Index)}";
                        Rectangle Code_Rectangle = new Rectangle(85, 1023, 200, 20);                        
                        //The cards rarity will also affect the colour that the set code is written in
                        //Normally this would be a "using" statement but since the colour changes at runtime
                        //I will have to manually dispose of the brush object
                        SolidBrush Rarity_Brush;
                        switch (c.Rarity.Trim())
                        {
                            //TODO add a method to these that lets me draw an outline around the text as it is drawn so nomatter
                            //the colour it is visible
                            //The rarity of the card determines the colour that the set code is drawn in
                            //higher rarity have more of a metalic effect (gold, silver and bronze) whilst the more common
                            //cards have red, blue and black 
                            case "Legendary":
                                //Legendary cards have gold writing
                                Rarity_Brush = new SolidBrush(ColorTranslator.FromHtml("#FFD700"));
                                break;
                            case "Ultra":
                                //Ultra cards have silver writing
                                Rarity_Brush = new SolidBrush(ColorTranslator.FromHtml("#DFDFDF"));
                                break;
                            case "Super":
                                //Super cards have bronze writing
                                Rarity_Brush = new SolidBrush(ColorTranslator.FromHtml("#CD7F32"));
                                break;
                            case "Rare":
                                //Rare cards have soviet red writing
                                Rarity_Brush = new SolidBrush(ColorTranslator.FromHtml("#FF1A00"));
                                break;
                            case "Uncommon":
                                //Uncommon Cards have patriot blue writing
                                Rarity_Brush = new SolidBrush(ColorTranslator.FromHtml("#103A5D"));
                                break;
                            default:
                                //By default common cards have black writing
                                Rarity_Brush = new SolidBrush(Color.Black);
                                break;
                        }
                        //if the card isn't common it needs to have its code outlined in black to make it easier to read
                        if(Rarity_Brush.Color != Color.Black)
                        {
                            //First outline the code in black so it is always readable regardless of colour
                            canvas = Functions.Outline_Text(canvas, Set_Code, Code_Rectangle, Card.CODE_FONT, true, false);
                        }
                        //the code gets its own font since monospacing is important
                        canvas = Functions.Write_Text(canvas, Set_Code, Code_Rectangle, false, Card.CODE_FONT, true, false, Rarity_Brush);
                        Rarity_Brush.Dispose();                        
                        //The last section is to draw on the little image of the artists signature is to be drawn in the bottom 
                        //since this is again an image file we need to make sure it exist
                        if (File.Exists(c.Signature))
                        {
                            using(Bitmap sig = new Bitmap(c.Signature))
                            {
                                //the width and height of the box can be any size they want, as long as they stay in the cards area
                                //the safe area that is, which starts at 75,75
                                int Signature_Start_X = Math.Max(80, Card.Signature_X - sig.Width);
                                int Signature_Start_Y = Math.Max(80, Card.Signature_Y - sig.Height);
                                //We then draw the signature onto this rectangle in such a way that
                                //it is right justified to the box it would be drawn in
                                canvas = Functions.Overlay_Image(canvas, sig, new Rectangle(Signature_Start_X, Signature_Start_Y, sig.Width, sig.Height));                                
                            }
                        }
                        //And like that all the information has been drawn to the card and we can safely return the image
                    }
                }
            }
            else
            {
                //if the artwork doesnt exist just fill up a new rectangle of the needed size with
                //pure green and return that
                canvas = new Bitmap(Card.BLEED_X, Card.BLEED_Y);
                using(Graphics g = Graphics.FromImage(canvas))
                {
                    g.FillRectangle(Brushes.Green, 0, 0, Card.BLEED_X, Card.BLEED_Y);
                }
            }
            //Lastly we just need to set the DPI meta data on the image so things like InDesign and Photoshop size it properly
            //print quality is 300 dpi
            canvas.SetResolution(300, 300);
            //Now whatever was actually done to the data we return canvas
            return canvas;
        }

        //Produce a card image without the bleed from a provided card
        public static Bitmap Generate_Cropped_Image(Card c,string set, string boxes)
        {
            //To keep the method simple delegate most of the work to generate bleed image as it saves a lot of 
            //code duplication
            Bitmap cc;
            using(Bitmap b = Generate_Bleed_Image(c, set, boxes))
            {
                //we simply crop it down to the size needed 
                cc = Functions.Crop_Image(b, Card.CROPPED_X, Card.CROPPED_Y);
                //making sure to reset the image DPI property to print quality
                cc.SetResolution(300, 300);
            }
            //and return it
            return cc;
        }
    }
}
