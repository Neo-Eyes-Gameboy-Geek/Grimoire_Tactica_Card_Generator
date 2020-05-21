using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Grimoire_Tactica_Card_Generator
{
    class Functions
    {
        //These are functions that aren't related specifically to the window but will be used in more than one place        

        //Crops a provided images to the largest integer scale of the dimensions provided
        //from the center 
        //Useful for trimming the full bleed off card images once generated
        public static Bitmap Crop_Image(Bitmap source, int x, int y)
        {
            //first we need the width and height of the image we have been provided
            int Source_Width = source.Width;
            int Source_Height = source.Height;
            //now get the largest integer scale of these dimensions
            //Flooring isn't necessary as both components are integers and integer division will always 
            //truncate to an integer
            int Width_Scale = Source_Width / x;
            //Ditto for the height
            int Height_Scale = Source_Height / y;
            //Now we want the smallest value of these two since thats the largest 
            //integer scale both dimensions of the image support
            int Scale = Math.Min(Width_Scale, Height_Scale);
            //and as an extra precaution for if the image supplied is of a smaller
            //resoloution than the box dimensions provided (Scale = 0)
            //Make sure that the Scale is at least 1
            Scale = Math.Max(Scale, 1);
            //We then need to use a bitmap of a size that is the scaled up
            //version of the dimensions we received earlier
            Bitmap canvas = new Bitmap(x * Scale, y * Scale);
            //And to accomodate letterboxing will the canvas with black
            using(Graphics g = Graphics.FromImage(canvas))
            {
                using (SolidBrush brush = new SolidBrush(Color.Black))
                {
                    g.FillRectangle(brush, new RectangleF(0, 0, canvas.Width, canvas.Height));
                }
            }
            //Now where we have to find the coordinate on the source that we will start
            //the copy from
            //this will either be half the difference between the original image and the canvas
            //or zero if the canvas is larger in that dimension than the source, which happens if Scale would
            //normally be 0
            int Source_Start_X = Math.Max(0, (Source_Width - canvas.Width) / 2);
            int Source_Start_Y = Math.Max(0, (Source_Height - canvas.Height) / 2);
            //We also need to get the width and height of the section we will be copying
            //Normally this would just be the canvas's width and height but in the event the image
            //is smaller than the canvas this would cause out of memory exceptions
            int Draw_Width = Math.Min(canvas.Width, Source_Width);
            int Draw_Height = Math.Min(canvas.Height, Source_Height);
            //And from these get where to start drawing on the canvas the cropped image
            //this will normally be (0,0) but images smaller than the canvas need centering
            int Canvas_Start_X = Math.Max(0, (canvas.Width - Draw_Width) / 2);
            int Canvas_Start_Y = Math.Max(0, (canvas.Height - Draw_Height) / 2);
            //now that we have all this information, lastly we construct a pair of rectangles, one for canvas and one 
            //for the source 
            RectangleF Source_Rectangle = new RectangleF(Source_Start_X, Source_Start_Y, Draw_Width, Draw_Height);
            RectangleF Canvas_Rectangle = new RectangleF(Canvas_Start_X, Canvas_Start_Y, Draw_Width, Draw_Height);
            //Now that we have all of this information we can finally draw the cropped section onto the canvas
            using(Graphics g = Graphics.FromImage(canvas))
            {
                //Setup g so that the results are as nice as possible
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                //and then draw the cropped area onto the canvas
                g.DrawImage(source, Canvas_Rectangle, Source_Rectangle, GraphicsUnit.Pixel);
            }
            return canvas;
        }

        //Scales a given image so that the smaller of its dimensions is 
        //constrained to one of the dimensions required, maintaining aspect ratio
        public static Bitmap Scale_Image(Bitmap i, int x, int y)
        {    
            //First of all we need to get whichever of the dimensions, comparative to the scale we
            //have been asked to do , has the larger scale since we will want to use that
            //the larger scale will cause less shrinkage and hopefully less image degradation
            //need to get cast the width and height to double to get a non rounded ratio
            double Source_Scale_X = ((double)x) / i.Width;
            double Source_Scale_Y = ((double)y) / i.Height;            
            double Scale = Math.Max(Source_Scale_X, Source_Scale_Y);
            //Now we need to get what size image this scale will create
            //in concrete pixel units, ceiling the value means that fractional
            //values will skew more towards the larger side rather than the smaller
            int Canvas_Width = (int)Math.Ceiling(i.Width * Scale);
            int Canvas_Height = (int)Math.Ceiling(i.Height * Scale);            
            //The bitmap we will be returning will be sized according
            //to the pixel sizes just calculated
            Bitmap canvas = new Bitmap(Canvas_Width, Canvas_Height);
            //and make sure DPI doesnt change so as to avoid some quality losses
            //Now we have the details we need we can make a graphics object and draw
            //the scaled image
            Rectangle Canvas_Rectangle = new Rectangle(0, 0, Canvas_Width, Canvas_Height);
            using(Graphics g = Graphics.FromImage(canvas))
            {
                //Set the quality features of the graphics object to ensure best quality scaling
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                //Lastly we need an image attributes object to reduce edge ghosting on the image once it is scaled down
                using(ImageAttributes a = new ImageAttributes())
                {
                    a.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    //Now draw the scaled image remembering to scale the whole image to that size 
                    g.DrawImage(i, Canvas_Rectangle, 0, 0, i.Width, i.Height, GraphicsUnit.Pixel, a);
                }
            }
            //Now all the drawing is done we can return the finished product
            return canvas;            
        }

        //This will from a provided String draw the requisite icon on bitmap provided at a predetermined location
        public static Bitmap Draw_Icon(Bitmap i, String s)
        {
            //This basically boils down to a switch block that compares the string against several other strings and
            //uses the image overlay to draw it into the correct location on the card            
            //All of the icons are in a subdirectory of the same folder as the execution
            switch (s)
            {
                case "Vanguard":
                    using(Bitmap b = new Bitmap(@".\Icons\Vanguard.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(124, 604, 60, 60));
                    }
                    break;
                case "Support":
                    using (Bitmap b = new Bitmap(@".\Icons\Support.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(227, 604, 60, 60));
                    }
                    break;
                case "Range":
                    using (Bitmap b = new Bitmap(@".\Icons\Range.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(330, 604, 60, 60));
                    }
                    break;
                case "Flyer":
                    using (Bitmap b = new Bitmap(@".\Icons\Flyer.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(433, 604, 60, 60));
                    }
                    break;
                case "Mage":
                    using (Bitmap b = new Bitmap(@".\Icons\Mage.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(536, 604, 60, 60));
                    }
                    break;
                case "Psychic":
                    using (Bitmap b = new Bitmap(@".\Icons\Psychic.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(639, 604, 60, 60));
                    }
                    break;
                case "Vehicle":
                    using (Bitmap b = new Bitmap(@".\Icons\Vehicle.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(639, 604, 60, 60));
                    }
                    break;
                case "Accessory":
                    using (Bitmap b = new Bitmap(@".\Icons\Accessory.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(536, 604, 60, 60));
                    }
                    break;
                case "Shield":
                    using (Bitmap b = new Bitmap(@".\Icons\Shield.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(433, 604, 60, 60));
                    }
                    break;
                case "Weapon":
                    using (Bitmap b = new Bitmap(@".\Icons\Weapon.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(330, 604, 60, 60));
                    }
                    break;
                case "Armour":
                    using (Bitmap b = new Bitmap(@".\Icons\Armour.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(227, 604, 60, 60));
                    }
                    break;
                case "Helmet":
                    using (Bitmap b = new Bitmap(@".\Icons\Helmet.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(124, 604, 60, 60));
                    }
                    break;
                case "Spell":
                    using (Bitmap b = new Bitmap(@".\Icons\Spell.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(330, 604, 60, 60));
                    }
                    break;
                case "Psychopomp":
                    using (Bitmap b = new Bitmap(@".\Icons\PsychoPomp.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(433, 604, 60, 60));
                    }
                    break;
                case "QuickPlay":
                    using (Bitmap b = new Bitmap(@".\Icons\QuickPlay.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(433, 604, 60, 60));
                    }
                    break;
                case "Continuious":
                    using (Bitmap b = new Bitmap(@".\Icons\Continuious.png"))
                    {
                        Overlay_Image(i, b, new Rectangle(330, 604, 60, 60));
                    }
                    break;
                default:
                    break;
            }
            return i;
        }

        //This will draw text to be as large as possible, in the font provided . in the middle of a rectangle
        //at a given posistion that is of a given size
        public static Bitmap Write_Text(Bitmap i, string s, Rectangle r, bool centered, string font, bool bold, bool italic)
        {
            //Since we want to avoid out of bounds errors make sure the rectangle remains within the bounds of the bitmap
            //and only execute if it does
            if(r.X >= 0 && r.Y >= 0 &&(r.X+r.Width < i.Width) && (r.Y + r.Height < i.Height))
            {
                //Step one is to make a graphics object that will draw the text in place
                using (Graphics g = Graphics.FromImage(i))
                {
                    //Set some of the graphics properties so that the text renders nicely
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    //Compositing Mode can't be set since string needs source over to be valid
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    //And an additional step to make sure text is proper anti-aliased and takes advantage
                    //of clear type as necessary
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    //this also requires a font object we need to make sure we dispose of properly
                    using (Font f = Functions.Generate_Font(s, font, r, bold, italic))
                    {
                        //the using function actually makes sure the font is as large as it can be for the 
                        //purpose of fitting the rectangle we just need to check if its centered
                        using (StringFormat format = new StringFormat())
                        {
                            //the format won't always be doing anything but
                            //just in case we need it
                            //and if the text is centered we need to tell the formatting
                            if (centered)
                            {
                                format.Alignment = StringAlignment.Center;
                                format.LineAlignment = StringAlignment.Center;
                            }
                            //and draw the text into place
                            g.DrawString(s, f, Brushes.Black, r, format);
                        }
                    }
                }
            }
            return i;
        }

        //And a slight variation for the method for doing so for a whole bunch of text, text wrapped to the rectangle provided
        //with some minor rich text additions, bolding will be handled automatically but sometimes all the font may bolded so it
        //stays for posterity however you have to top left align the text
        public static Bitmap Write_Rich_Text(Bitmap i, string s, Rectangle r, string font, bool bold, bool italic, Brush brush)
        {
            //First of all we need a graphics object to draw all of this
            using(Graphics g = Graphics.FromImage(i))
            {                
                //Make sure the graphics draw nice
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                //Compositing Mode can't be set since string needs source over to be valid
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                //And an additional step to make sure text is proper anti-aliased and takes advantage
                //of clear type as necessary
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                //now we need two fonts, one for regular text and another for bold words
                using (Font regf = Generate_Font(s, font, r, bold, italic, r.Width))
                {
                    using(Font bolf = new Font(regf, System.Drawing.FontStyle.Bold))
                    {
                        //we then need to split the string up into individual words
                        //since the words will have to be draw word for word
                        string[] Words = s.Split(new char[] { ' ' , '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        //as well as a pair of variable to track the posistion of the "cursor" and the right most bound 
                        float Cursor_X = r.X;
                        float Cursor_X_Origin = r.X;
                        float Cursor_Y = r.Y;
                        //the cursor cannot draw outside the bounds of the box after all
                        float Cursor_Bound_X = r.X + r.Width;
                        float Cursor_Bound_Y = r.Y + r.Height;
                        //and an increment for the font when we need to new line
                        //since typefaces are the same height across characters we just measure one
                        //although this probably isnt as accurate as the kerning from drawstring we need to make
                        //do here since drawing rich text isnt something done by an existing method
                        //flooring the value also keeps the strings a little closer together which shouldn't be an issue
                        int Line_Height = (int)Math.Floor(g.MeasureString("A", regf).Height);
                        foreach (string word in Words)
                        {                            
                            //first off we need to make sure the cursor is still within the box and writing a new word wont push us out
                            //since we cant draw outside it
                            if (Cursor_Y < Cursor_Bound_Y)
                            {
                                //The font we will be drawing in just to know which to use
                                Font f;
                                //since we cant alter a for each iteration variable store it
                                string target = string.Copy(word);
                                //first things first we need to see if the word meets requirements to be bolded                                
                                if (target.Contains("[") && target.Contains("]"))
                                {
                                    //if the text is boldable make the text bold
                                    f = bolf;
                                    //and remove the braces since we don't want to draw 
                                    //them and it recovers us some width
                                    //strings are immutable so we need to do the trimming on a new 
                                    //string and then trade references
                                    string temp = target.Replace("[", "").Replace("]", "");
                                    target = temp;
                                }
                                else
                                {
                                    //if it isnt we use regular font
                                    f = regf;
                                }
                                //Now we just need to make sure we can are still fit within current line
                                //if we can't advance the line and draw it there
                                //normally we would need to check that drawing it wouldn't overrun the line 
                                //but the text wrapping in the font generation automatically assures us that it will not
                                if(Cursor_X > Cursor_Bound_X || Cursor_X + g.MeasureString(target, regf).Width > Cursor_Bound_X)
                                {
                                    //If it is we return the cursor to the start and drop a line
                                    Cursor_X = Cursor_X_Origin;
                                    Cursor_Y += Line_Height;
                                }
                                //Now draw the text drawing outside the box could be an issue but the font generator already
                                //assured us we are in bounds in the y
                                g.DrawString(target, f, brush, Cursor_X, Cursor_Y);
                                //and then advance the cursors x posistion by the width plus a space
                                //so words dont smoosh together
                                //The regular font is what is used when determining font size so move the cursor by that                               
                                Cursor_X += g.MeasureString($"{target} ", regf).Width;                               
                            }
                        }
                    }
                }
            }
            return i;
        }
        
        //This function simply takes the parameters given to it and and produces the largest font it can
        //for a given string in a given rectangle
        public static Font Generate_Font(string s,string font_family, Rectangle r, bool bold, bool italic)
        {
            //First things first, the font can't be of a size larger than the rectangle in pixels so 
            //we need to find the smaller dimension as that will constrain the max size
            int Max_Size = Math.Min(r.Width, r.Height);
            //Now we loop backwards from this max size until we find a size of font that fits inside the 
            //rectangle given
            for(int size = Max_Size; size > 0; size--)
            {
                //Since a default font is used if the font family specified doesnt exist 
                //checking the family exists isnt necessary
                //However we need to cover if the font is bold or italic
                Font f;
                if (bold)
                {
                    f = new Font(font_family, size, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
                }
                else if (italic)
                {
                    f = new Font(font_family, size, System.Drawing.FontStyle.Italic, GraphicsUnit.Pixel);
                }
                else if (bold && italic)
                {
                    //the pipe is a bitwise or and plays with the enum flags to get both bold and italic 
                    f = new Font(font_family, size, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic, GraphicsUnit.Pixel);
                }
                else
                {
                    //otherwise make a simple font
                    f = new Font(font_family, size, GraphicsUnit.Pixel);
                }
                //because graphics are weird we need a bitmap and graphics object to measure the string
                //we also need a sizef to store the measured results
                SizeF result;
                using(Bitmap b = new Bitmap(100,100))
                {
                    using(Graphics g = Graphics.FromImage(b))
                    {
                        result = g.MeasureString(s, f);
                    }
                }
                //if the new string fits the constraints of the rectangle we return it
                if(result.Width<= r.Width && result.Height <= r.Height)
                {
                    return f;
                }
                //if it didnt we dispose of f and try again
                f.Dispose();
            }
            //If something goes horribly wrong and no font size fits just return comic sans in 12 pt font
            //that won't upset anyone and the rectangle it will be drawn to will clip the excess anyway
            return new Font("Comic Sans", 12, GraphicsUnit.Point);
        }

        //A function overload of the above that makes the dont, wrapped to the given width rather than all on 1 line
        public static Font Generate_Font(string s, string font_family, Rectangle r, bool bold, bool italic, int width)
        {
            //First things first, the font can't be of a size larger than the rectangle in pixels so 
            //we need to find the smaller dimension as that will constrain the max size
            int Max_Size = Math.Min(r.Width, r.Height);
            //Now we loop backwards from this max size until we find a size of font that fits inside the 
            //rectangle given
            for (int size = Max_Size; size > 0; size--)
            {
                //Since a default font is used if the font family specified doesnt exist 
                //checking the family exists isnt necessary
                //However we need to cover if the font is bold or italic
                Font f;
                if (bold)
                {
                    f = new Font(font_family, size, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
                }
                else if (italic)
                {
                    f = new Font(font_family, size, System.Drawing.FontStyle.Italic, GraphicsUnit.Pixel);
                }
                else if (bold && italic)
                {
                    //the pipe is a bitwise or and plays with the enum flags to get both bold and italic 
                    f = new Font(font_family, size, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic, GraphicsUnit.Pixel);
                }
                else
                {
                    //otherwise make a simple font
                    f = new Font(font_family, size, GraphicsUnit.Pixel);
                }
                //because graphics are weird we need a bitmap and graphics object to measure the string
                //we also need a sizef to store the measured results
                SizeF result;
                using (Bitmap b = new Bitmap(10, 10))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        result = g.MeasureString(s, f, width);                        
                    }
                }
                //if the new string fits the constraints of the rectangle we return it
                if (result.Width <= r.Width && result.Height <= r.Height)
                {                    
                    return f;
                }
                //if it didnt we dispose of f and try again
                f.Dispose();
            }
            //If something goes horribly wrong and no font size fits just return comic sans in 12 pt font
            //that won't upset anyone and the rectangle it will be drawn to will clip the excess anyway
            return new Font("Comic Sans", 12, GraphicsUnit.Point);
        }

        //And a method to draw, one image onto another image as is, cropped and scaled to the 
        //rectangle provided, although not scaled as nicely as an image from the scale function
        public static Bitmap Overlay_Image(Bitmap target, Bitmap source, Rectangle r)
        {
            //Step one is to make sure the rectangle remains in bounds of the target
            if((r.Width + r.X) < target.Width && (r.Height + r.Y) < target.Height)
            {
                //now we make a graphics object out of the target
                using(Graphics g = Graphics.FromImage(target))
                {
                    //Set the graphics to be as nice as possible
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    //source over needs to be used here since without it transparent pixels render as black
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                    //And draw the source onto the target in the rectangle provided
                    //the method provides a simple upscale if its needed, not as nice as
                    //the scale method but it gets there
                    g.DrawImage(source, r, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
                }
            }
            return target;
        }

        //This saves the contents of a card image into the textfile associated with it
        public static void Save_Card(Card c)
        {
            //First of all we need to get the filepath for the file this is going to be written to
            FileInfo file = c.Text_File;
            string path = file.FullName;
            //We now need to open a file writer for this path so we can line for line write the contents into 
            //the file overwriting whatever was in there before
            try
            {
                //never can be too careful when dealing with opening and writing files , reading is usually less a concern
                //since multiple reads are possible simultaneously in a lot of cases
                using(StreamWriter sw = new StreamWriter(path))
                {
                    //Now its simply a matter of writing the values to the file in the correct order
                    sw.WriteLine(c.Name);
                    sw.WriteLine(c.Title);
                    sw.WriteLine(c.Cost);
                    sw.WriteLine(c.Skills);
                    sw.WriteLine(c.Keywords);
                    sw.WriteLine(c.HP);
                    sw.WriteLine(c.ATK);
                    sw.WriteLine(c.DEF);
                    //Abilities as ever require a little extra work 
                    foreach(string s in c.Abilities)
                    {
                        sw.WriteLine(s);
                    }
                    sw.WriteLine(Card.END_ABILITIES_STRING);
                    sw.WriteLine(c.Flavour_Text);
                    sw.WriteLine(c.Signature);
                    sw.WriteLine(c.Artwork);                    
                    //This will then close the stream writer, freeing the file for use elsewhere
                }
                //and update the atached files name to match the cards name and title
                string new_file_name = $"{c.Name}_{c.Title}";
                //sanitise the string so as not to crash the program
                string regexSearch = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars());
                Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
                new_file_name = r.Replace(new_file_name, "");
                //now obviously we can only rename the file if a file by that name doesnt already exist
                //or the file isnt already named as it needs to be
                if (!File.Exists(file.Directory + $@"\{new_file_name}.txt") && file.Name != $"{new_file_name}.txt")
                {
                    file.MoveTo($@"{file.Directory}\{new_file_name}.txt");                    
                }
            }
            catch(Exception e)
            {
                MessageBox.Show($"An Error Occured In The Program: {e}\nAs such the file could not be saved!");
            }
        }

        //This just does the above to an entire list of cards
        public static void Save_Set(List<Card> cards)
        {
            //Save these cards in parallel since all the work should be thread safe
            //and over large sets of cards this is a significant speed up
            Parallel.ForEach(cards, c => { Save_Card(c); });            
        }
    }
}
