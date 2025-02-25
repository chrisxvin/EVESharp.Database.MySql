// Copyright (c) 2012, 2021, Oracle and/or its affiliates.
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

using EVESharp.Database.MySql.Authentication.SSPI;
using System;
using EVESharp.Database.MySql;

namespace EVESharp.Database.MySql.Authentication;

/// <summary>
/// Allows connections to a user account set with the authentication_windows authentication plugin.
/// </summary>
internal class MySqlWindowsAuthenticationPlugin : MySqlAuthenticationPlugin
{
    private string              targetName      = null;
    private SspiSecurityContext securityContext = null;

    protected override void CheckConstraints ()
    {
        string platform = string.Empty;

        int p = (int) Environment.OSVersion.Platform;

        if (p == 4 || p == 128)
            platform = "Unix";
        else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
            platform = "Mac OS/X";

        if (!string.IsNullOrEmpty (platform))
            throw new MySqlException (string.Format (Resources.WinAuthNotSupportOnPlatform, platform));

        base.CheckConstraints ();
    }

    public override string GetUsername ()
    {
        string username = this.Settings.UserID;

        if (string.IsNullOrEmpty (username))
            return "auth_windows";

        return username;
    }

    public override string PluginName => "authentication_windows_client";

    protected override byte [] MoreData (byte [] moreData)
    {
        if (moreData == null)
            this.securityContext = new SspiSecurityContext (new SspiCredentials ("Negotiate"));

        ContextStatus status = this.securityContext.InitializeSecurityContext (out byte [] clientBlob, moreData, this.targetName);

        if (status == ContextStatus.Accepted)
            this.securityContext.Dispose ();

        return clientBlob;
    }
}