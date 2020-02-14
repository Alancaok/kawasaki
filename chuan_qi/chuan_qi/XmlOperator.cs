using System;
using System.Xml;

namespace test
{
    class XmlOperator
    {
        private string xmlFilePath;
        private XmlDocument doc;

        public XmlOperator(string xmlString)
        {
            doc = new XmlDocument();
            string innerText = xmlString;
            doc.LoadXml(innerText);
        }
        
        public string[] GetFirstChildNodeByTagName(string tagName)
        {
            string[] commands = new string[5];
            XmlNodeList xList = doc.GetElementsByTagName(tagName);
            XmlNode firstChildNode = xList[0].FirstChild;
            Console.WriteLine(firstChildNode.InnerText);
            string innerAttr;
            try
            {
                innerAttr = ((XmlElement)firstChildNode).GetAttribute("n");
            }
            catch (Exception e)
            {
                innerAttr="";
            }
            commands[0] = firstChildNode.Name;
            if (!innerAttr.Equals(""))
                commands[1] = innerAttr;
            return commands;
        }
        

    }
}
