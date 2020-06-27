using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EdcsServer.Helper
{
    public interface IModelHelper
    {
        public T DeserializeJson<T>(string json);
        public string SerializeJson<T>(T model);
    }

    class ModelHelper : IModelHelper
    {
        public T DeserializeJson<T>(string json)
        {
            if (String.IsNullOrEmpty(json))
                throw new ArgumentNullException();
            else
                return JsonConvert.DeserializeObject<T>(json);
        }

        public string SerializeJson<T>(T model)
        {
            if (model != null)
                return JsonConvert.SerializeObject(model);
            else
                throw new ArgumentNullException();
        }
    }
}
