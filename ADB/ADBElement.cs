using System.Drawing;
using System.Xml;

namespace ADB
{
    public class ADBElement
    {
        public XmlNode Xml { get; private set; }
        public ADBDevice Device { get; private set; }
        public Point ElementPos { get; private set;}
        public Size ElementSize { get; private set; }


        public string Content { get => Xml.Value; private set { } }
        public string Text { get => Xml.Attributes["text"].Value; private set { } }
        public XmlAttributeCollection Attributes { get => Xml.Attributes; private set { } }



        public ADBElement(ADBDevice device, XmlNode xml, Point elementPos, Size elementSize)
        {
            this.Device         = device;
            this.Xml            = xml;
            this.ElementPos     = elementPos;
            this.ElementSize    = elementSize;
        }


        public void Click(uint milliseconds = 0)
        {
            this.Device.Click(new Point() { X = (ElementPos.X + (ElementSize.Width / 2)), Y = (ElementPos.Y + (ElementSize.Height / 2)) }, milliseconds);
        }
    }
}
