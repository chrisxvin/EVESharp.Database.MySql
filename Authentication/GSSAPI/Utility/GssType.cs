﻿// Copyright (c) 2020, Oracle and/or its affiliates.
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

using EVESharp.Database.MySql;
using System.Text;
using EVESharp.Database.MySql.Authentication.GSSAPI.Native;

namespace EVESharp.Database.MySql.Authentication.GSSAPI.Utility;

internal static class GssType
{
    private static readonly Encoding Iso = Encoding.GetEncoding ("iso-8859-1");

    internal static Disposable <GssBufferDescStruct> GetBufferFromString (string buffer)
    {
        return GetBufferFromBytes (Iso.GetBytes (buffer));
    }

    public static Disposable <GssBufferDescStruct> GetBufferFromBytes (byte [] buffer)
    {
        return Disposable.From (
            Pinned.From (buffer), p => new GssBufferDescStruct
            {
                length = (uint) p.Value.Length,
                value  = p.Address
            }, p =>
            {
                uint majorStatus = NativeMethods.gss_release_buffer (out uint minorStatus, ref p);

                if (majorStatus != Const.GSS_S_COMPLETE)
                    throw new MySqlException (
                        ExceptionMessages.FormatGssMessage (
                            "GSSAPI: An error occurred releasing a buffer.",
                            majorStatus, minorStatus, Const.GSS_C_NO_OID
                        )
                    );
            }
        );
    }
}