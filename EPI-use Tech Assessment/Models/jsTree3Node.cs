using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace jsTree3.Models
{
    public class JsTree3Node
    {
        public string id { get; set; }
        public string parent { get; set; }
        public string text { get; set; }
        public string icon { get; set; }

        public State state;

        public string li_attr { get; set; }
        public string a_attr { get; set; }


    }

    public class State
    {
        public bool opened = false;
        public bool disabled = false;
        public bool selected = false;

        public State(bool Opened, bool Disabled, bool Selected)
        {
            opened = Opened;
            disabled = Disabled;
            selected = Selected;
        }
    }

}