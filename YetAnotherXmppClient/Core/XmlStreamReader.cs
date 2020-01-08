using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Core
{
    public enum XmlPartType
    {
        None,
        Unknown,
        ProcessingInstruction,
        XmlDeclaration,
        OpeningTag,
        ClosingTag,
        OpeningBracket,
        Element
    }
    public interface IXmlPart
    {
        string RawXml { get; }
        string Name { get; }
        XmlPartType PartType { get; }
    }

    public abstract class XmlPartBase : IXmlPart
    {
        public string RawXml { get; protected set; }
        public string Name { get; protected set; }
        public abstract XmlPartType PartType { get; }
    }

    public class XmlOpeningTag : XmlPartBase, IXmlPart
    {
        public override XmlPartType PartType => XmlPartType.OpeningTag;
        public string Name { get; }
        public Dictionary<string,string> Attributes { get; } = new Dictionary<string, string>();

        public XmlOpeningTag(string name) => this.Name = name;
    }
    class XmlClosingTag : XmlPartBase, IXmlPart
    {
        public override XmlPartType PartType => XmlPartType.ClosingTag;
    }
    class XmlElement : XmlPartBase, IXmlPart
    {
        public override XmlPartType PartType => XmlPartType.Element;

        public XmlElement(string name, string rawXml)
        {
            this.Name = name;
            this.RawXml = rawXml;
        }
    }

    public class XmlStreamReader
    {
        private readonly StreamReader streamReader;

        public XmlStreamReader(Stream serverStream)
        {
            this.streamReader = new StreamReader(serverStream);
        }

        enum ParsingState
        {
            
        }

        enum State
        {
            None,
            ProcessingInstruction,
            TagName,
            AttributeName,
            AttributeValue
        }
        public async Task<XmlOpeningTag> ReadOpeningTagAsync()
        {
            State state = State.None;
            char c, lastChar = (char)0;
            var sb = new StringBuilder();
            string attrName = null;
            XmlOpeningTag openingTag = null;
            do
            {
                c = await this.streamReader.ReadCharAsync();

                if (state == State.ProcessingInstruction)
                {
                    if (c == '>' && state == State.ProcessingInstruction)
                    {
                        state = State.None;
                        c = ' '; //UNDONE
                        lastChar = ' ';
                    }

                    continue;
                }

                if (c == '\n' || c == '\r')
                {
                    continue;
                }
                if (c == '?' && lastChar == '<')
                {
                    // "<?"
                    state = State.ProcessingInstruction;
                    continue;
                }
                else if (c == '>')
                {
                    Expect(() => lastChar != '/');
                    if (state == State.TagName)
                    {
                        return new XmlOpeningTag(sb.ToString());
                    }
                    if (state == State.AttributeValue)
                    {
                        var attrValue = sb.ToString().Trim('\'', '\"');
                        openingTag.Attributes.Add(attrName, attrValue);
                    }
                    else
                    {
                        throw new Exception("invalid xml");
                    }
                }
                else if (c == ' ')
                {
                    if (state == State.TagName)
                    {
                        openingTag = new XmlOpeningTag(sb.ToString());
                    }
                    else if (state == State.AttributeValue)
                    {
                        var attrValue = sb.ToString().Trim('\'', '\"');
                        openingTag.Attributes.Add(attrName, attrValue);
                    }

                    state = State.None;

                    sb.Clear();
                }
                else if (c == '=')
                {   // Attribute name ends
                    Expect(() => state == State.AttributeName);
                    attrName = sb.ToString();
                    sb.Clear();
                    state = State.AttributeValue;
                }

                if (lastChar == '=')
                {
                    sb.Clear();
                }

                //if (sb.ToString().Length == 0)
                //{   // '<' at beginning
                //    Expect(() => c == '<');
                //}
                //if (sb.ToString().Length > 0)
                //{
                //    // No more '<'
                //    Expect(() => c != '<');
                //}

                if (lastChar == '<')
                {
                    Expect(() => char.IsLetter(c) || c == '_');
                    state = State.TagName;
                    sb.Clear();
                }
                else if (lastChar == ' ' && (char.IsLetter(c) || c == '_'))
                {
                    sb.Clear();
                    state = State.AttributeName;
                }
            
                sb.Append(c);
                lastChar = c;
            } while (c != '>' || state == State.ProcessingInstruction);

            return openingTag;
        }

        public async Task<IXmlPart> ReadElementOrClosingTagAsync()
        {
            XmlPartType currentPartType = XmlPartType.Unknown;
            IXmlPart currentPart = null;
            var sb = new StringBuilder();
            var nameSb = new StringBuilder();
            char c;
            char lastChar = (char)0;
            var depth = 0;
            while (true)
            {
                var readOp = await this.streamReader.TryReadCharAsync();
                if(!readOp.Success)
                    throw new ArgumentOutOfRangeException("read 0 bytes");
                c = readOp.Char;
                
                //Expect '<' at the beginning
                if (sb.ToString().Length == 0)
                {
                    Expect(() => c == '<');
                }

                bool ignore = currentPartType == XmlPartType.ProcessingInstruction
                                || c == '\n' || c == '\r';
                if (!ignore)
                {
                    sb.Append(c);
                    nameSb.Append(c);
                    Debug.WriteLine(sb);
                }

                switch (c)
                {
                    case '<':
                    {
                        currentPartType = XmlPartType.Unknown;
                        nameSb.Clear();
                        break;
                    }
                    case '>' when lastChar == '/':
                    {
                        // "/>"
                        --depth;
                        if (depth == 0)
                        {
                            return new XmlElement(null, sb.ToString());
                        }
                        break;
                    }
                    case '>' when currentPartType == XmlPartType.ClosingTag:
                    {
                        // End of closing tag
                        --depth;
                        if (depth == 0)
                        {
                            return new XmlElement(null, sb.ToString());
                        }
                        else if (depth == -1)
                        {
                            return (XmlClosingTag) currentPart;
                        }
                        break;
                    }
                    case '>' when currentPartType == XmlPartType.ProcessingInstruction:
                    {
                        //End of processing instruction
                        Expect(() => lastChar == '?'); //Processing instruction must end with "?>"
                        currentPartType = XmlPartType.None;
                        break;
                    }
                    case '/' when lastChar == '<':
                    {   
                        // "</"
                        currentPartType = XmlPartType.ClosingTag;
                        currentPart = new XmlClosingTag();
                        nameSb.Clear();
                        break;
                    }
                    case '?' when lastChar == '<':
                    {
                        // "<?"
                        currentPartType = XmlPartType.ProcessingInstruction;
                        break;
                    }
                    case '\r':
                    case '\n':
                    {
                        ignore = true;
                        //UNDONE kann es in einem namen vorkommen?
                        break;
                    }
                }

                if (lastChar == '<' && (char.IsLetter(c) || c == '_'))
                {
                    currentPartType = XmlPartType.OpeningTag;
                    ++depth;
                }
                

                lastChar = c;
                
            }
            return null;
        }

    }

    static class StreamReaderExtensions
    {
        public static async Task<(bool Success, char Char)> TryReadCharAsync(this StreamReader streamReader)
        {
            var buffer = new char[1];
            if (0 == await streamReader.ReadAsync(buffer, 0, 1))
//                throw new InvalidOperationException("read 0 bytes");
                return (false, (char)0);
            return (true, buffer[0]);
        }
        
        public static async Task<char> ReadCharAsync(this StreamReader streamReader)
        {
            var buffer = new char[1];
            if (0 == await streamReader.ReadAsync(buffer, 0, 1))
                throw new InvalidOperationException("read 0 bytes");;
            return buffer[0];
        }
    }
}
