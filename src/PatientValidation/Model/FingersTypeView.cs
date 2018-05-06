using EmiratesId.AE.Biometrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PatientValidation.Model
{
    public class FingersTypeView
    {
        public int Id { get; set; }

        public FingerIndexType Type { get; set; }

        public string Name { get; set; }
    }
}
