// Copyright (c) 2004, 2022, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System.IO;
using System.Reflection;
using System.Text;

namespace EVESharp.Database.MySql;

internal class Utils
{
    public static string ReadResource (string name)
    {
        string rez = ReadResourceInternal (name);

        if (rez != null)
            return rez;

        return ReadResourceInternal ("MySqlClient/" + name);
    }

    public static string ReadResourceInternal (string name)
    {
        Assembly assembly = Assembly.GetExecutingAssembly ();

        string resName = assembly.GetName ().Name + "." + name.Replace (" ", "_")
                                                              .Replace ("\\", ".")
                                                              .Replace ("/",  ".");

        Stream resourceStream = assembly.GetManifestResourceStream (resName);

        if (resourceStream == null)
            return null;

        using (StreamReader reader = new StreamReader (resourceStream, Encoding.UTF8))
        {
            return reader.ReadToEnd ();
        }
    }

    /// <summary>
    /// Removes the outer backticks and replace the double-backticks to single-backtick
    /// of inside the quotedString.
    /// </summary>
    /// <param name="quotedString">The string to unquote.</param>
    /// <returns></returns>
    public static string UnquoteString (string quotedString)
    {
        if (quotedString.StartsWith ("`"))
            return quotedString.Substring (1, quotedString.Length - 2).Replace ("``", "`");
        else
            return quotedString;
    }
}