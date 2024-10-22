using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ArtonitRESTAPI.Legasy_Service
{
    public class DatabaseResult
    {
        public State State { get; set; }

        public object Value { get; set; }

        public string ErrorMessage { get; set; }

    }
    public enum State
    {
        Successes,//200
        BadSQLRequest,//400
        NullSQLRequest,//404
        NullDataBase,//500
    }

}
