﻿// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class AuthTests : TestBase
  {
    protected TestSetup ts;

    public AuthTests(TestSetup setup) : base (setup, "authtests")
    {
      ts = setup;
    }


    [Fact]
    public void TestIntegratedSecurityNoPoolingWithoutUser()
    {
      TestIntegratedSecurityWithUser(null, false);
    }

    [Fact]
    public void TestIntegratedSecurityPoolingWithoutUser()
    {
      TestIntegratedSecurityWithUser(null, true);
    }

    public void TestIntegratedSecurityWithUser(string user, bool pooling)
    {
      const string PluginName = "authentication_windows";
      string UserName = "auth_windows";
      if (user != null)
        UserName = user;

      // Check if server has windows authentication plugin is installed		
      MySqlDataReader reader = ExecuteAsReader("show plugins", ts.GetConnection(true));

      bool haveWindowsAuthentication = false;
      using (reader)
      {
        if (reader.HasRows)
        {
          while (reader.Read())
          {
            string name = (string)reader["Name"];
            if (name == PluginName)
            {
              haveWindowsAuthentication = true;
              break;
            }
          }
        }
      }
      if (!haveWindowsAuthentication)
        return;

      bool haveAuthWindowsUser = false;
      string pluginName = null;
      string authenticationString = "";

      // Check if predefined proxy user exists
      string sql = string.Format("select plugin, authentication_string from mysql.user where user='{0}'", UserName);
      using (MySqlDataReader reader2 = ExecuteAsReader(sql, ts.GetConnection(true)))
      {
        if (reader2.Read())
        {
          haveAuthWindowsUser = true;
          pluginName = (string)reader2["plugin"];
          authenticationString =
            (string)((reader2["authentication_string"] == DBNull.Value) ?
            "" : reader2["authentication_string"]);
        }
      }

      // Create mapping for current Windows user=>foo_user
      String windowsUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
      windowsUser = windowsUser.Replace("\\", "\\\\");
      string userMapping = windowsUser + "=foo_user";

      if (!haveAuthWindowsUser)
      {
        executeAsRoot(
          "CREATE USER " + UserName + " IDENTIFIED WITH " + PluginName + " as '" +
           userMapping + "'");
      }
      else
      {
        // extend mapping string for current user
        executeAsRoot(
          "UPDATE mysql.user SET authentication_string='" + userMapping +
          "," + authenticationString + "' where user='" + UserName + "'");
      }
      executeAsRoot("create user foo_user identified by 'pass'");
      executeAsRoot("grant all privileges on *.* to 'foo_user'@'%'");
      executeAsRoot("grant proxy on foo_user to " + UserName);


      // Finally, use IntegratedSecurity=true for the newly created user
      string connStr = ts.GetConnection(true).ConnectionString + ";Integrated Security=SSPI";

      MySqlConnectionStringBuilder sb =
          new MySqlConnectionStringBuilder(connStr);
      sb.UserID = user;
      connStr = sb.ConnectionString;

      /* If pooling is requested, we'll  run test twice, with connection reset in between */
      if (pooling)
      {
        connStr += ";Connection Reset=true;Pooling=true";
      }
      int testIterations = pooling ? 2 : 1;

      int threadId = -1;
      for (int i = 0; i < testIterations; i++)
      {
        using (MySqlConnection c = new MySqlConnection(connStr))
        {
          c.Open();
          threadId = c.ServerThread;
          MySqlCommand command = new MySqlCommand("SELECT 1", c);
          long ret = (long)command.ExecuteScalar();
          Assert.Equal(ret, 1);

          command.CommandText = "select user()";
          string myUser = (string)command.ExecuteScalar();
          // Check if proxy user is correct
          Assert.True(myUser.StartsWith(UserName + "@"));

          // check if mysql user is correct 
          // (foo_user is mapped to current  OS user)
          command.CommandText = "select current_user()";
          string currentUser = (string)command.ExecuteScalar();
          Assert.True(currentUser.StartsWith("foo_user@"));
        }
      }

      if (pooling)
      {
        executeAsRoot("KILL " + threadId);
      }
    }

    [Fact]
    public void TestIntegratedSecurityNoPoolingWithUser()
    {
      TestIntegratedSecurityWithUser("myuser1", false);
    }

    [Fact]
    public void TestIntegratedSecurityPoolingWithUser()
    {
      TestIntegratedSecurityWithUser("myuser1", true);
    }

    public void TestIntegratedSecurityWithoutProxy(string user, bool pooling)
    {
      const string PluginName = "authentication_windows";
      string UserName = "auth_windows";
      if (user != null)
        UserName = user;

      var conn = ts.GetConnection(true); 
      
      if (conn.State != ConnectionState.Open)
          conn.Open();

        // Check if server has windows authentication plugin is installed			
        MySqlCommand cmd = new MySqlCommand("show plugins", conn);

        bool haveWindowsAuthentication = false;
        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          while (r.Read())
          {
            string name = (string)r["Name"];
            if (name == PluginName)
            {
              haveWindowsAuthentication = true;
              break;
            }
          }
        }

      conn.Close();

        if (!haveWindowsAuthentication)
          return;      

        bool haveAuthWindowsUser = false;
        string pluginName = null;
        string authenticationString = "";

        // Check if predefined proxy user exists
        cmd.CommandText = string.Format(
          "select plugin, authentication_string from mysql.user where user='{0}'",
          UserName);
        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          if (r.Read())
          {
            haveAuthWindowsUser = true;
            pluginName = (string)r["plugin"];
            authenticationString =
              (string)((r["authentication_string"] == DBNull.Value) ?
              "" : r["authentication_string"]);
          }
        }      

      // Create mapping for current Windows user=>foo_user
      String windowsUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
      windowsUser = windowsUser.Replace("\\", "\\\\");
      string userMapping = "fergs, Administrators";

      try
      {
        if (!haveAuthWindowsUser)
        {
          executeAsRoot(
            "CREATE USER " + UserName + " IDENTIFIED WITH " + PluginName + " as '" +
             userMapping + "'");
        }
        else
        {
          // extend mapping string for current user
          executeAsRoot(
            "UPDATE mysql.user SET authentication_string='" + userMapping +
            "," + authenticationString + "' where user='" + UserName + "'");
        }
        executeAsRoot(string.Format("grant all privileges on *.* to '{0}'@'%'", UserName));

        
        // Finally, use IntegratedSecurity=true for the newly created user
        string connStr = ts.GetConnection(true).ConnectionString + ";Integrated Security=SSPI";

        MySqlConnectionStringBuilder sb =
            new MySqlConnectionStringBuilder(connStr);
        sb.UserID = user;
        connStr = sb.ConnectionString;

        /* If pooling is requested, we'll  run test twice, with connection reset in between */
        if (pooling)
        {
          connStr += ";Connection Reset=true;Pooling=true";
        }
        int testIterations = pooling ? 2 : 1;

        int threadId = -1;
        for (int i = 0; i < testIterations; i++)
        {
          using (MySqlConnection c = new MySqlConnection(connStr))
          {
            c.Open();
            threadId = c.ServerThread;
            MySqlCommand command = new MySqlCommand("SELECT 1", c);
            long ret = (long)command.ExecuteScalar();
            Assert.Equal(ret, 1);

            command.CommandText = "select user()";
            string myUser = (string)command.ExecuteScalar();
            // Check if proxy user is correct
            Assert.True(myUser.StartsWith(UserName + "@"));

            // check if mysql user is correct 
            // (foo_user is mapped to current  OS user)
            command.CommandText = "select current_user()";
            string currentUser = (string)command.ExecuteScalar();
            Assert.True(currentUser.StartsWith(UserName));
          }
        }

        if (pooling)
        {
          executeAsRoot("KILL " + threadId);
        }
      }
      finally
      {
        // Cleanup

        // Drop test user
        executeAsRoot(string.Format("drop user {0}", UserName));
      }
    }

    [Fact]
    public void TestWinAuthWithoutProxyNoUserNoPooling()
    {
      TestIntegratedSecurityWithoutProxy(null, false);
    }

    [Fact]
    public void TestWinAuthWithoutProxyNoUserPooling()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", true);
    }

    [Fact]
    public void TestWinAuthWithoutProxyAndUser()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", false);
    }

    [Fact]
    public void TestWinAuthWithoutProxyAndUserPooling()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", true);
    }

  }
}