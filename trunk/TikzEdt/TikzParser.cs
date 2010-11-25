﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Antlr.Runtime;
using Antlr.Runtime.Tree;

namespace TikzEdt
{
    /// <summary>
    /// 
    /// </summary>
    static class TikzParser
    {
        public static Tikz_ParseTree Parse(string code)
        {
            
            simpletikzLexer lex = new simpletikzLexer(new ANTLRStringStream(code));
            CommonTokenStream tokens = new CommonTokenStream(lex);

            //for (int i = 0; i < tokens.Count; i++)
            //{
            //    string ds = tokens.Get(i).Text;
            //    ds = ds + "eee";
            //}

            simpletikzParser parser = new simpletikzParser(tokens);

            //tikzgrammarParser.expr_return r =
            simpletikzParser.tikzdocument_return ret = parser.tikzdocument();
            
            //CommonTreeAdaptor adaptor = new CommonTreeAdaptor();
            CommonTree t = (CommonTree)ret.Tree;
            //MessageBox.Show(printTree(t,0));



        

/*
        public string printTree(CommonTree t, int indent)
        {
            string s="";
            if ( t != null ) {
		        for ( int i = 0; i < indent; i++ )
			        s = s+"   ";

                string r = "";// s + t.ToString() + "\r\n";
                
                if (t.ChildCount >0)
		            foreach ( object o in t.Children ) {
			            r=r+s+o.ToString()+"\r\n" + printTree((CommonTree)o, indent+1);
                    }

                return r;
            }  else return "";
		}
    }*/
            
            
            Tikz_ParseTree root = new Tikz_ParseTree();

            bool success = FillItem(root, t, tokens);


            // mockup 
            //t.Children.Add(new Tikz_Node(0, 0));
            //t.Children.Add(new Tikz_Node(5, 5));
            //t.Children.Add(new Tikz_Node(6, 8));
            //t.Children.Add(new Tikz_Node(8, 8));
            if (success)
                return root;
            else
                return null;
        }

        static bool FillItem(TikzContainerParseItem item, CommonTree t, CommonTokenStream tokens)
        {
            int curItem = t.TokenStartIndex;

            foreach (CommonTree childt in t.Children)
            {
                addSomething(item, tokens, curItem, childt.TokenStartIndex-1);
                
                switch (childt.Type)
                {
                    case simpletikzParser.IM_PICTURE:
                        Tikz_Picture tp = new Tikz_Picture();
                        FillItem(tp, childt, tokens);
                        item.Children.Add(tp);
                        break;
                    case simpletikzParser.IM_STARTTAG:
                        item.starttag = getTokensString(tokens, childt.TokenStartIndex, childt.TokenStopIndex);
                        break;
                    case simpletikzParser.IM_ENDTAG:
                        item.endtag = getTokensString(tokens, childt.TokenStartIndex, childt.TokenStopIndex);
                        break;
                    case simpletikzParser.IM_PATH:
                        Tikz_Path tpath = new Tikz_Path();
                        FillItem(tpath, childt, tokens);
                        item.Children.Add(tpath);
                        break;
                    case simpletikzParser.IM_SCOPE:
                        Tikz_Scope tscope = new Tikz_Scope();
                        FillItem(tscope, childt, tokens);
                        item.Children.Add(tscope);
                        break;
                    case simpletikzParser.IM_COORD:
                        Tikz_Coord tc = Tikz_Coord.FromCommonTree(childt);
                        tc.text = getTokensString(tokens, childt);
                        item.Children.Add(tc);
                        break;
                    case simpletikzParser.IM_NODE:
                        Tikz_Node tn = Tikz_Node.FromCommonTree(childt);
                        tn.text = getTokensString(tokens, childt);
                        item.Children.Add(tn);
                        break;
                    default:
                        // getting here is an error
                        break;

                }

                curItem = childt.TokenStopIndex + 1;               

            }


            return true;
        }

        public static string getTokensString(CommonTokenStream tokens, CommonTree t)
        {
            return getTokensString(tokens, t.TokenStartIndex, t.TokenStopIndex);
        }
        public static string getTokensString(CommonTokenStream tokens, int FirstToken, int LastToken)
        {
            if (LastToken - FirstToken <= 0)
                return "";
            string text = "";
            for (int i = FirstToken; i <= LastToken; i++)
            {
                text = text + tokens.Get(i).Text;
            }
            return text;
        }

        static void addSomething(TikzContainerParseItem item, CommonTokenStream tokens, int FirstToken, int LastToken)
        {
            if (LastToken-FirstToken <= 0)
                return;

            Tikz_Something t = new Tikz_Something(getTokensString(tokens, FirstToken, LastToken));
            item.Children.Add(t);            
        }

   /*     static List<string[]> DoubleSplit(string toSplit, string starttag, string endtag)
        {
            string[] startsplit = toSplit.Split(new string[] { starttag }, StringSplitOptions.None);
            List<string[]> l = new List<string[]>();

            int curdepth = 0;
            string curcontent="";
            foreach (string s in startsplit)
            {
                if (curdepth == 0)
                {
                    l.Add(new string[] { s });
                    curdepth++;
                }
                else
                {
                    string[] endsplit = s.Split(new string[] { endtag }, StringSplitOptions.None);
                    foreach (string t in endsplit)
                    {
                        if (curdepth == 0)
                            l.Add(new string[] { starttag, t, endtag });
                        else


                            curdepth--;
                    }
                }
            }
        }
        */
    }

    public class TikzParseItem
    {
        public string text = "";
        public TikzParseItem(string txt)
        {
            text = txt;
        }
        public TikzParseItem()
        {
        }
        public override string ToString()
        {
            return text;
        }
    }
    /// <summary>
    /// This item represents parts of the code that the parser does not understand
    /// or not care about, e. g., whitespace.
    /// </summary>
    public class Tikz_Something : TikzParseItem
    {
        public Tikz_Something(string txt)
        {
            text = txt;
        }
        public Tikz_Something()
        {
        }
    }
    /// <summary>
    /// Parse Item having an x and y coordinate
    /// </summary>
    public class Tikz_XYItem : TikzParseItem
    {
        public double x, y;
    }
    public class Tikz_Node : Tikz_XYItem
    {
        public static Tikz_Node FromCommonTree(CommonTree t)
        {
            // IM_NODE OPTIONS? nodename? coord? STRING
            Tikz_Node n = new Tikz_Node();
            int i = 0;
            if (t.GetChild(i).Type == simpletikzParser.OPTIONS)
            {
                n.options = t.GetChild(i++).Text;
            }
            if (t.GetChild(i).Type == simpletikzParser.IM_NODENAME)
            {
                n.name = t.GetChild(i++).GetChild(0).Text;
            }
            if (t.GetChild(i).Type == simpletikzParser.IM_COORD)
            {
                n.coord = Tikz_Coord.FromCommonTree(t.GetChild(i++));
                n.x = n.coord.uX.number; //hack
                n.y = n.coord.uY.number;
            }
            n.label = t.GetChild(i).Text;

            return n;
        }

        public Tikz_Coord coord;
        public string name="";
        public string options="";
        public string label = "";
        public Tikz_Node() { }
        public Tikz_Node(double tx, double ty)
        {
            x=tx; y=ty;
        }
    }
    public enum Tikz_CoordType { Cartesian, Polar, Named }
    //public enum Tikz_CoordDeco { none, p, pp }
    public class Tikz_Coord : Tikz_XYItem
    {
        public static Tikz_Coord FromCommonTree(ITree t)
        {
            Tikz_Coord tc = new Tikz_Coord();
            if (t.ChildCount == 1 && t.GetChild(0).Type == simpletikzParser.IM_NODENAME) // named node 
            {
                tc.type = Tikz_CoordType.Named;
                tc.nameref = t.GetChild(0).GetChild(0).Text;
                return tc;
            } 
            else if (t.ChildCount >= 2) 
            {
                int i = 0;
                if (t.ChildCount == 3)
                {
                    tc.deco = t.GetChild(0).Text;
                    i = 1;
                }
                tc.uX = new Tikz_NumberUnit(t.GetChild(i));
                tc.uY = new Tikz_NumberUnit(t.GetChild(i+1));

                tc.x = tc.uX.number; // hack
                tc.y = tc.uY.number;

                return tc;
            }

            return null;
        }

        public Tikz_CoordType type = Tikz_CoordType.Cartesian;
        public string deco = "";
        public Tikz_NumberUnit uX, uY;
        public string nameref = ""; // name of vertex if coordinate is such a reference
        public Tikz_Coord() { }
        public Tikz_Coord(double tx, double ty)
        {
            x = tx; y = ty;
        }
    }
    public class Tikz_NumberUnit
    {
        public Tikz_NumberUnit(ITree t)
        {
            number = Double.Parse(t.GetChild(0).Text);
            if (t.ChildCount > 1)
                unit = t.GetChild(1).Text;
        }
        public double number;
        public string unit;
    }
    public class TikzContainerParseItem : TikzParseItem
    {
        public string starttag="", endtag="";
        public List<TikzParseItem> Children= new List<TikzParseItem>();
        public override string ToString()
        {
            string s = starttag;
            foreach (TikzParseItem t in Children)
            {
                s = s + t.ToString();
            }
            return s+endtag;
        }
    }
    // the root of the parse tree
    public class Tikz_ParseTree : TikzContainerParseItem
    {

    }
    public class Tikz_Draw : TikzContainerParseItem
    {

    }
    public class Tikz_Picture : TikzContainerParseItem
    {

    }
    public class Tikz_Path : TikzContainerParseItem
    {

    }
    public class Tikz_Scope : TikzContainerParseItem
    {

    }
}