using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TSKT
{
    public class Nml
    {
        readonly static char[] trimCharacters = new char[] { ' ', '\n', '\r', '\t' };

        string name;
        List<string> parameters;
        List<Nml> children;
        bool accessed = false;
        Nml parent;

        public string Name
        {
            get
            {
                accessed = true;
                return name;
            }
        }

        public List<string> Parameters
        {
            get
            {
                accessed = true;
                return parameters;
            }
        }

        public List<Nml> Children
        {
            get
            {
                accessed = true;
                return children;
            }
        }

        public void Parse(string src)
        {
            ParseInternal(src, 0, false);
        }

        int ParseInternal(string src, int index, bool requestCloseBracket)
        {
            children = new List<Nml>();

            var builder = new StringBuilder();
            bool comment = false;

            var currentChild = new Nml
            {
                parent = this
            };
            var processingName = true;

            while(index < src.Length)
            {
                var currentChar = src[index];
                if (comment)
                {
                    if (currentChar == '\n')
                    {
                        comment = false;
                    }
                }
                else
                {
                    if (currentChar == '/')
                    {
                        if (index + 1 < src.Length)
                        {
                            if (src[index + 1] == '/')
                            {
                                comment = true;
                            }
                        }
                    }
                    else if (currentChar == '{')
                    {
                        if (processingName)
                        {
                            currentChild.name = builder.ToString().Trim(trimCharacters).Trim('"');
                        }
                        else
                        {
                            if (currentChild.parameters == null)
                            {
                                currentChild.parameters = new List<string>();
                            }
                            currentChild.parameters.Add(builder.ToString().Trim(trimCharacters).Trim('"'));
                        }
                        builder.Length = 0;
                        children.Add(currentChild);
                        index = currentChild.ParseInternal(src, index + 1, requestCloseBracket: true);

                        currentChild = new Nml
                        {
                            parent = this
                        };
                        processingName = true;
                    }
                    else if (currentChar == '}')
                    {
                        if (builder.ToString().Trim(trimCharacters).Length > 0)
                        {
                            UnityEngine.Assertions.Assert.IsTrue(false, "syntax error! : " + builder.ToString().Trim(trimCharacters));
                        }
                        if (!requestCloseBracket)
                        {
                            UnityEngine.Assertions.Assert.IsTrue(false, "予期しない括弧閉じです : " + src);
                        }
                        break;
                    }
                    else if (currentChar == ';')
                    {
                        if (processingName)
                        {
                            currentChild.name = builder.ToString().Trim(trimCharacters).Trim('"');
                        }
                        else
                        {
                            if (currentChild.parameters == null)
                            {
                                currentChild.parameters = new List<string>();
                            }
                            currentChild.parameters.Add(builder.ToString().Trim(trimCharacters).Trim('"'));
                        }
                        builder.Length = 0;
                        children.Add(currentChild);

                        currentChild = new Nml
                        {
                            parent = this
                        };
                        processingName = true;
                    }
                    else if (currentChar == ' ' || currentChar == '\t')
                    {
                        if (processingName)
                        {
                            var nameSource = builder.ToString().Trim(trimCharacters);
                            if (!string.IsNullOrEmpty(nameSource))
                            {
                                currentChild.name = nameSource.Trim('"');
                                builder.Length = 0;
                                processingName = false;
                            }
                        }
                        else
                        {
                            builder.Append(currentChar);
                        }
                    }
                    else if (currentChar == ',')
                    {
                        if (processingName)
                        {
                            builder.Append(currentChar);
                        }
                        else
                        {
                            if (currentChild.parameters == null)
                            {
                                currentChild.parameters = new List<string>();
                            }
                            currentChild.parameters.Add(builder.ToString().Trim(trimCharacters).Trim('"'));
                            builder.Length = 0;
                        }
                    }
                    else if (currentChar == '"')
                    {
                        var end = src.IndexOf('"', index + 1);
                        UnityEngine.Assertions.Assert.AreNotEqual(-1, end);
                        if (end >= 0)
                        {
                            builder.Append(src.Substring(index, end - index + 1));
                            index = end;
                        }
                    }
                    else
                    {
                        builder.Append(currentChar);
                    }
                }
                ++index;
            }
            if (index == src.Length)
            {
                if (requestCloseBracket)
                {
                    UnityEngine.Assertions.Assert.IsTrue(false, "括弧が閉じられていません");
                }
            }
            return index;
        }

        public Nml SearchChild(string name)
        {
            accessed = true;

            if (children == null)
            {
                return null;
            }
            bool Func(Nml _) => (_.name.ToLowerInvariant() == name.ToLowerInvariant());
            UnityEngine.Assertions.Assert.IsTrue(children.Count(Func) <= 1, "has multi children : " + name);
            return children.FirstOrDefault(Func);
        }

        public bool HasChild(string name)
        {
            if (children == null)
            {
                return false;
            }
            return children.Any(_ => _.name.ToLowerInvariant() == name.ToLowerInvariant());
        }

        public T GetParameter<T>(string name)
        {
            UnityEngine.Assertions.Assert.IsNotNull(SearchChild(name), "not found : " + FullPath + ", " + name);
            UnityEngine.Assertions.Assert.IsNotNull(SearchChild(name).Parameters, "not found : " + FullPath + ", " + name);

            return TryGetParameter(name, default(T));
        }

        public IEnumerable<Nml> SearchChildren(string name)
        {
            accessed = true;
            return children.Where(_ => _.name.ToLowerInvariant() == name.ToLowerInvariant());
        }

        public T TryGetParameter<T>(string name, T defaultValue)
        {
            accessed = true;
            var attr = SearchChild(name);
            if (attr == null)
            {
                return defaultValue;
            }

            if (attr.Parameters == null)
            {
                return defaultValue;
            }

            UnityEngine.Assertions.Assert.AreEqual(attr.Parameters.Count, 1, "パラメータ数が不正です : " + string.Join(", ", attr.Parameters.ToArray()));

            if (typeof(T).IsEnum)
            {
                UnityEngine.Assertions.Assert.IsFalse(string.IsNullOrEmpty(attr.parameters[0]), "parameter is null or empty in " + name);
                return (T)System.Enum.Parse(typeof(T), attr.parameters[0]);
            }

            return (T)System.Convert.ChangeType(attr.parameters[0], typeof(T));
        }

        public void CheckIfAccessedAllElements()
        {
            UnityEngine.Assertions.Assert.IsTrue(accessed, "error: ignored child " + FullPath);
            if (children != null)
            {
                foreach (var it in children)
                {
                    it.CheckIfAccessedAllElements();
                }
            }
        }

        string FullPath
        {
            get
            {
                var layers = new List<string>();

                var pos = this;
                while (pos != null)
                {
                    var n = pos.name;
                    if (pos.parameters != null && pos.parameters.Count > 0)
                    {
                        n += " ";
                        n += string.Join(", ", pos.parameters.ToArray());
                    }
                    layers.Add(n);
                    pos = pos.parent;
                }

                return string.Join("/", layers.Reverse<string>().ToArray());
            }
        }
    }
}
