using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Models
{
    [Serializable]
    public  class Login
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
