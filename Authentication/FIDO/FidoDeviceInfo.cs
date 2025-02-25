﻿// Copyright (c) 2022, Oracle and/or its affiliates.
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
using EVESharp.Database.MySql.Authentication.FIDO.Native;
using EVESharp.Database.MySql.Authentication.FIDO.Utility;

namespace EVESharp.Database.MySql.Authentication.FIDO;

/// <summary>
/// A class representing external info about a particular FIDO capable device
/// </summary>
internal sealed unsafe class FidoDeviceInfo : IDisposable
{
    private          fido_dev_info_t* _devInfo;
    private readonly fido_dev_info_t* _current;
    private readonly int              _size = 5;

#region Properties

    /// <summary>
    /// Gets the manufacturer of the device
    /// </summary>
    public string Manufacturer => NativeMethods.fido_dev_info_manufacturer_string (this._current);

    /// <summary>
    /// Gets the path of the device (for use in <see cref="FidoDevice.Open(string)"/>)
    /// </summary>
    public string Path => NativeMethods.fido_dev_info_path (this._current);

    /// <summary>
    /// Gets the product ID of the device
    /// </summary>
    public short Product => NativeMethods.fido_dev_info_product (this._current);

    /// <summary>
    /// Gets a string representation of the product ID
    /// </summary>
    public string ProductString => NativeMethods.fido_dev_info_product_string (this._current);

    /// <summary>
    /// Gets the vendor ID of the device
    /// </summary>
    public short Vendor => NativeMethods.fido_dev_info_vendor (this._current);

#endregion

#region Constructors

    static FidoDeviceInfo ()
    {
        Init.Call ();
    }

    internal FidoDeviceInfo ()
    {
        this._devInfo = NativeMethods.fido_dev_info_new ((IntPtr) this._size);

        if (this._devInfo == null)
            throw new OutOfMemoryException ();

        IntPtr olen = IntPtr.Zero;
        NativeMethods.fido_dev_info_manifest (this._devInfo, (IntPtr) this._size, &olen).Check ();

        for (int i = 0; i < (int) olen; i++)
        {
            this._current = NativeMethods.fido_dev_info_ptr (this._devInfo, (IntPtr) i);

            if (this.Product != 1)
                break;
        }
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    ~FidoDeviceInfo ()
    {
        this.ReleaseUnmanagedResources ();
    }

#endregion

    private void ReleaseUnmanagedResources ()
    {
        if (this._devInfo == null)
            return;

        fido_dev_info_t* native = this._devInfo;
        NativeMethods.fido_dev_info_free (&native, (IntPtr) this._size);
        this._devInfo = null;
    }

#region IDisposable

    public void Dispose ()
    {
        this.ReleaseUnmanagedResources ();
        GC.SuppressFinalize (this);
    }

#endregion
}