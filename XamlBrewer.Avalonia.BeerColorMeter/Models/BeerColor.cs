namespace XamlBrewer.Avalonia.BeerColorMeter.Models
{
    using global::Avalonia.Media;
    using System.Linq;

    public class BeerColor
    {
        public double SRM { get; set; }

        public double EBC
        {
            get
            {
                return 1.97 * this.SRM;
            }
        }

        public byte R { get; set; }

        public byte G { get; set; }

        public byte B { get; set; }

        public string ColorName
        {
            get
            {
                return (from c in DAL.BeerColorGroups
                        where c.MaximumSRM >= this.SRM
                        select c.ColorName).FirstOrDefault();
            }
        }

        public Color Color
        {
            get
            {
                return new Color(225, R, G, B);
            }
        }
    }
}