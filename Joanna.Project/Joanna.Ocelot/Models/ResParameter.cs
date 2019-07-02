using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Joanna.Ocelot.Models
{
    public class ResParameter
    {
        /// <summary>
        /// 接口响应码
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// 接口响应消息
        /// </summary>
        public string info { get; set; }
        /// <summary>
        /// 接口响应数据
        /// </summary>
        public object data { get; set; }
    }
}
