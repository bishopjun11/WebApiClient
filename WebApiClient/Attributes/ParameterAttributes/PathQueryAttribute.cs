﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApiClient.Contexts;
using WebApiClient.Interfaces;

namespace WebApiClient.Attributes
{
    /// <summary>
    /// 使用KeyValueFormatter序列化参数值得到的键值对作为url路径参数或query参数的特性
    /// 没有任何特性修饰的参数，将默认被PathQueryAttribute修饰
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class PathQueryAttribute : Attribute, IApiParameterAttribute
    {
        /// <summary>
        /// 时期时间格式
        /// </summary>
        private readonly string datetimeFormate;

        /// <summary>
        /// 编码
        /// </summary>
        private Encoding encoding = System.Text.Encoding.UTF8;

        /// <summary>
        /// 获取或设置参数的编码
        /// </summary>
        public string Encoding
        {
            get
            {
                return this.encoding.WebName;
            }
            set
            {
                this.encoding = System.Text.Encoding.GetEncoding(value);
            }
        }

        /// <summary>
        /// 获取或设置当值为null是此参数
        /// 默认为false
        /// </summary>
        public bool IgnoreWhenNull { get; set; }

        /// <summary>
        /// 表示Url路径参数或query参数的特性
        /// </summary>
        public PathQueryAttribute()
            : this(null)
        {
        }

        /// <summary>
        /// 表示Url路径参数或query参数的特性
        /// </summary>
        /// <param name="datetimeFormat">时期时间格式</param>
        public PathQueryAttribute(string datetimeFormat)
        {
            this.datetimeFormate = datetimeFormat;
        }

        /// <summary>
        /// http请求之前
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="parameter">特性关联的参数</param>
        /// <exception cref="HttpApiConfigException"></exception>
        /// <returns></returns>
        public async Task BeforeRequestAsync(ApiActionContext context, ApiParameterDescriptor parameter)
        {
            var uri = context.RequestMessage.RequestUri;
            if (uri == null)
            {
                throw new HttpApiConfigException("未配置HttpConfig.HttpHost或未使用HttpHostAttribute特性");
            }

            if (this.WillIgnore(parameter.Value) == false)
            {
                var fixUrl = uri.ToString().TrimEnd('?', '&', '/');
                var options = context.HttpApiConfig.FormatOptions.CloneChange(this.datetimeFormate);
                var keyValues = context.HttpApiConfig.KeyValueFormatter.Serialize(parameter, options);
                var targetUrl = new Uri(this.UsePathQuery(fixUrl, keyValues));
                context.RequestMessage.RequestUri = targetUrl;
                await ApiTask.CompletedTask;
            }
        }

        /// <summary>
        /// url添加query
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="keyValues">键值对</param>
        /// <returns></returns>
        private string UsePathQuery(string url, IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            foreach (var keyValue in keyValues)
            {
                url = this.UsePathQuery(url, keyValue);
            }
            return url;
        }

        /// <summary>
        /// url添加query
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="keyValue">键值对</param>
        /// <returns></returns>
        private string UsePathQuery(string url, KeyValuePair<string, string> keyValue)
        {
            var key = keyValue.Key;
            var value = keyValue.Value ?? string.Empty;
            var regex = new Regex("{" + key + "}", RegexOptions.IgnoreCase);

            if (regex.IsMatch(url) == true)
            {
                return regex.Replace(url, value);
            }

            var valueEncoded = HttpUtility.UrlEncode(value, this.encoding);
            var query = string.Format("{0}={1}", key, valueEncoded);
            var concat = url.Contains('?') ? "&" : "?";
            return url + concat + query;
        }

        /// <summary>
        /// 返回是否应该忽略提交 
        /// </summary>
        /// <param name="val">值</param>
        /// <returns></returns>
        private bool WillIgnore(object val)
        {
            return this.IgnoreWhenNull == true && val == null;
        }
    }
}
