﻿#region license
// boo - an extensible programming language for the CLI
// Copyright (C) 2004 Rodrigo Barreto de Oliveira
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// As a special exception, if you link this library with other files to
// produce an executable, this library does not by itself cause the
// resulting executable to be covered by the GNU General Public License.
// This exception does not however invalidate any other reasons why the
// executable file might be covered by the GNU General Public License.
//
// Contact Information
//
// mailto:rbo@acm.org
#endregion

using System;
using System.Collections;

namespace Boo.AntlrParser.Util
{
	public class SimpleToken : antlr.Token
	{
		protected string _buffer;

		public SimpleToken(int type, string txt) : base(type, txt)
		{
		}

		override public void setText(string txt)
		{
			_buffer = txt;
		}

		override public string getText()
		{
			return _buffer;
		}
	}

	/// <summary>
	/// Process whitespace tokens and generate INDENT, DEDENT
	/// virtual tokens as needed.
	/// </summary>
	public class IndentTokenStreamFilter : antlr.TokenStream
	{
		/// <summary>
		/// token input stream.
		/// </summary>
		protected antlr.TokenStream _istream;

		/// <summary>
		/// whitespace token type.
		/// </summary>
		protected int _wsTokenType;

		/// <summary>
		/// singleton indent token.
		/// </summary>
		protected int _indentTokenType;

		/// <summary>
		/// singleton dedent token.
		/// </summary>
		protected int _dedentTokenType;

		/// <summary>
		/// singleton EOS token.
		/// </summary>
		protected int _eosTokenType;

		/// <summary>
		/// stack of indent levels.
		/// </summary>
		protected Stack _indentStack;

		/// <summary>
		/// tokens waiting to be consumed
		/// </summary>
		protected Queue _pendingTokens;
		
		System.Text.StringBuilder _buffer = new System.Text.StringBuilder();

		public IndentTokenStreamFilter(antlr.TokenStream istream, int wsTokenType, int indentTokenType, int dedentTokenType, int eosTokenType)
		{
			if (null == istream)
			{
				throw new ArgumentNullException("istream");
			}

			_istream = istream;
			_wsTokenType = wsTokenType;
			_indentTokenType = indentTokenType;
			_dedentTokenType = dedentTokenType;
			_eosTokenType = eosTokenType;
			_indentStack = new Stack();
			_pendingTokens = new Queue();

			_indentStack.Push(0); // current indent level is zero
		}

		protected int CurrentIndentLevel
		{
			get
			{
				return (int)_indentStack.Peek();
			}
		}

		public antlr.Token nextToken()
		{
			if (0 == _pendingTokens.Count)
			{
				ProcessNextTokens();
			}
			return (antlr.Token)_pendingTokens.Dequeue();
		}
		
		void ProcessNextTokens()
		{		
			_buffer.Length = 0;
				
			antlr.Token token = null;
			while (true)
			{			
				token = _istream.nextToken();
				
				int ttype = token.Type;
				if (antlr.Token.SKIP == ttype)
				{
					continue;
				}
				
				if (_wsTokenType == ttype)
				{			
					_buffer.Append(token.getText());
					continue;
				}
				
				break;
			}
			
			if (0 != _buffer.Length)
			{
				string text = _buffer.ToString();
				string[] lines = text.Split('\r', '\n');					

				if (lines.Length > 1)
				{
					string lastLine = lines[lines.Length-1];
					if (lastLine.Length > CurrentIndentLevel)
					{
						EnqueueIndent(token);
						_indentStack.Push(lastLine.Length);
					}
					else if (lastLine.Length < CurrentIndentLevel)
					{
						EnqueueEOS(token);
						do 
						{
							EnqueueDedent(token);
							_indentStack.Pop();
						}
						while (lastLine.Length < CurrentIndentLevel);
					}
					else
					{
						EnqueueEOS(token);
					}
				}
			}
			
			if (antlr.Token.EOF_TYPE == token.Type)
			{					
				EnqueueEOS(token);	
				while (CurrentIndentLevel > 0)
				{
					EnqueueDedent(token);
					_indentStack.Pop();					
				}
			}
			
			Enqueue(token);
		}
		
		void Enqueue(antlr.Token token)
		{
			_pendingTokens.Enqueue(token);
		}

		void EnqueueIndent(antlr.Token originalToken)
		{
			_pendingTokens.Enqueue(CreateToken(originalToken, _indentTokenType, "<INDENT>"));
		}

		void EnqueueDedent(antlr.Token originalToken)
		{
			_pendingTokens.Enqueue(CreateToken(originalToken, _dedentTokenType, "<DEDENT>"));
		}		

		void EnqueueEOS(antlr.Token originalToken)
		{
			_pendingTokens.Enqueue(CreateToken(originalToken, _eosTokenType, "<EOS>"));
		}

		antlr.Token CreateToken(antlr.Token originalToken, int newTokenType, string newTokenText)
		{
			return new BooToken(originalToken, newTokenType, newTokenText);
		}
	}
}
