using System;
using System.Collections.Generic;

namespace MapEditor.Objects
{
    [Serializable]
    public class FractalObject
    {
        /// <summary>
        /// A special class for the object system to work.
        /// By default has:
        /// name - name of an object, will be seen in the UI;
        /// type - either STATIC (no controller) or DYNAMIC (has controller);
        /// defaultScale and defaultRotation - self explainatory;
        /// controller - the name of some class in Fractal Space that will be used;
        /// arguments - dictionary, where key is the argument name, and value is the, well, value.
        /// </summary>
        public string name;
        public string type; //ALWAYS STATIC or DYNAMIC.
        public float[] defaultScale;
        public float[] defaultRotation;
        public string controller;
        public Dictionary<string, string> arguments;
    }
}
