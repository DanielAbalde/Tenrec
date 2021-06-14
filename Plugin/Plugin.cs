using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Tenrec.Plugin
{ 

    public class TenrecInfo : GH_AssemblyInfo
    {
        public override string Name => "Tenrec";
        public override Bitmap Icon => null;
        public override string Description => "Unit Tests & Integration Tests as Grasshopper files";
        public override Guid Id => new Guid("a3dd5972-bd66-4a8e-81f9-c20ac8e23aaa");
        public override string AuthorName => "Daniel Gonzalez Abalde";
        public override string AuthorContact => "https://discord.gg/mRZj7xgTaC";
    }

    public class TenrecPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        { 
            Grasshopper.Instances.CanvasCreated += Instances_CanvasCreated;
            Grasshopper.Instances.CanvasDestroyed += Instances_CanvasDestroyed;
            return  GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasDestroyed(Grasshopper.GUI.Canvas.GH_Canvas canvas)
        {
            UI.CanvasLog.Instance.Dispose();
        }

        private void Instances_CanvasCreated(Grasshopper.GUI.Canvas.GH_Canvas canvas)
        {
            UI.CanvasLog.Instance = new UI.CanvasLog(canvas, 400, StringAlignment.Far);
        }
    }
}
