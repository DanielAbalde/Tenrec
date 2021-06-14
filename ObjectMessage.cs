using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tenrec
{
    /// <summary>
    /// Unit test states.
    /// </summary>
    public enum UnitTestState { Untested = -1, Failure = 0, Valid = 1 }

    /// <summary>
    /// Message entity to group some properties into the same object.
    /// </summary>
    public class ObjectMessage
    {
        public System.Drawing.RectangleF Bounds { get; set; }
        public string Name { get; private set; }
        public Grasshopper.Kernel.GH_RuntimeMessageLevel Level { get; set; }
        public IList<string> Messages { get; set; }
        public Grasshopper.Kernel.IGH_DocumentObject Object { get; set; }

        public ObjectMessage(string name, Grasshopper.Kernel.GH_RuntimeMessageLevel level, IList<string> messages, Grasshopper.Kernel.IGH_DocumentObject obj)
        {
            Name = name;
            Level = level;
            Messages = messages;
            Object = obj;
        }

        public override string ToString()
        {
            var txt = $"{Level} on {Name}! {string.Join(", ", Messages)}";
            if (!char.IsPunctuation(txt[txt.Length - 1]))
                txt = string.Concat(txt, ".");
            return txt;
        }
    }

}
