// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Globalization;
using EVESharp.Database.MySql;

namespace EVESharp.Database.MySql.Types;

internal struct MySqlInt64 : IMySqlValue
{
    public MySqlInt64 (bool isNull)
    {
        this.IsNull = isNull;
        this.Value  = 0;
    }

    public MySqlInt64 (long val)
    {
        this.IsNull = false;
        this.Value  = val;
    }

#region IMySqlValue Members

    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => MySqlDbType.Int64;

    object IMySqlValue.Value => this.Value;

    public long Value { get; }

    Type IMySqlValue.SystemType => typeof (long);

    string IMySqlValue.MySqlTypeName => "BIGINT";

    void IMySqlValue.WriteValue (MySqlPacket packet, bool binary, object val, int length)
    {
        long v = val as long? ?? Convert.ToInt64 (val);

        if (binary)
            packet.WriteInteger (v, 8);
        else
            packet.WriteStringNoNull (v.ToString (CultureInfo.InvariantCulture));
    }

    IMySqlValue IMySqlValue.ReadValue (MySqlPacket packet, long length, bool nullVal)
    {
        if (nullVal)
            return new MySqlInt64 (true);

        if (length == -1)
            return new MySqlInt64 ((long) packet.ReadULong (8));
        else
            return new MySqlInt64 (long.Parse (packet.ReadString (length), CultureInfo.InvariantCulture));
    }

    void IMySqlValue.SkipValue (MySqlPacket packet)
    {
        packet.Position += 8;
    }

#endregion

    internal static void SetDSInfo (MySqlSchemaCollection sc)
    {
        // we use name indexing because this method will only be called
        // when GetSchema is called for the DataSourceInformation 
        // collection and then it wil be cached.
        MySqlSchemaRow row = sc.AddRow ();
        row ["TypeName"]              = "BIGINT";
        row ["ProviderDbType"]        = MySqlDbType.Int64;
        row ["ColumnSize"]            = 0;
        row ["CreateFormat"]          = "BIGINT";
        row ["CreateParameters"]      = null;
        row ["DataType"]              = "System.Int64";
        row ["IsAutoincrementable"]   = true;
        row ["IsBestMatch"]           = true;
        row ["IsCaseSensitive"]       = false;
        row ["IsFixedLength"]         = true;
        row ["IsFixedPrecisionScale"] = true;
        row ["IsLong"]                = false;
        row ["IsNullable"]            = true;
        row ["IsSearchable"]          = true;
        row ["IsSearchableWithLike"]  = false;
        row ["IsUnsigned"]            = false;
        row ["MaximumScale"]          = 0;
        row ["MinimumScale"]          = 0;
        row ["IsConcurrencyType"]     = DBNull.Value;
        row ["IsLiteralSupported"]    = false;
        row ["LiteralPrefix"]         = null;
        row ["LiteralSuffix"]         = null;
        row ["NativeDataType"]        = null;
    }
}