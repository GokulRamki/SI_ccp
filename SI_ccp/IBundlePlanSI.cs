using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace SI_ccp
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IBundlePlanSI" in both code and config file together.
    [ServiceContract]
    public interface IBundlePlanSI
    {
        [OperationContract]
        void DoWork();
    }
}
