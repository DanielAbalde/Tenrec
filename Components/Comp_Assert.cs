
using Grasshopper.Kernel; 
using System; 
using System.Drawing; 

namespace Tenrec.Components
{
    public class Comp_Assert : GH_Component 
    {
        #region Properties
        protected override Bitmap Icon => Properties.Resources.assert_24x24;// ValidResult ? Properties.Resources.assert_24x24 : Properties.Resources.assert2_24x24;
        public override Guid ComponentGuid => new Guid("0a2b53e4-3d49-49b3-a8e4-92b7d2da382a");
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public bool ValidResult;
        #endregion

        #region Constructors 
        public Comp_Assert() : base("Assert", "Assert",
            "Ensure that a condition is met or throw an error.", "Params", "Util") { }
        #endregion

        #region Methods
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Assert", "A", "True if the condition has been met", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "M", "Assert messsage", GH_ParamAccess.item);
            Params.Input[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager){ }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ValidResult = false;
            string message = string.Empty;
            if (!DA.GetData(0, ref ValidResult))
            {
                if(RuntimeMessageLevel == GH_RuntimeMessageLevel.Blank)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Assert (A) failed to collect data");
                return;
            }

            if (!DA.GetData(1, ref message) && !ValidResult)
                message = $"{NickName} failed.";
             
            if(!ValidResult)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
        }
        #endregion
    }

}
