﻿// Copyright (c) Duende Software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Duende.IdentityModel.Client;

/// <summary>
/// Request for device authorization
/// </summary>
public class DeviceAuthorizationRequest : ProtocolRequest
{
    /// <summary>
    /// Space separated list of the requested scopes (optional).
    /// </summary>
    /// <value>
    /// The scope.
    /// </value>
    public string? Scope { get; set; }
}