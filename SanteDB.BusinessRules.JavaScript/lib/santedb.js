﻿/// <reference path="./santedb-model.js"/>
/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 * 
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: justin
 * Date: 2018-7-23
 */
'use strict';

// Backing of execution environment
var ExecutionEnvironment = {
    Unknown: 0,
    Server: 1,
    Mobile: 2,
    UserInterface: 3
};

if (!SanteDBWrapper)
    /**
     * @class
     * @constructor
     * @summary SanteDB Binding Class
     * @description This class exists as a simple interface which is implemented by host implementations of the SanteDB hostable core. This interface remains the same even though the 
     *              implementations of this file on each platform (Admin, BRE, Client, etc.) are different.
     */
    function SanteDBWrapper() {
        "use strict";

        var _viewModelJsonMime = "application/json+sdb-viewModel";

        /**
         * @summary Re-orders the JSON object properties so that $type appears as the first property
         * @param {any} object The object whose properites should be reordered
         * @returns {any} The appropriately ordered object
         */
        var _reorderProperties = function (object) {

            // Object has $type and $type is not the first property
            if (object.$type) {
                var retVal = { $type: object.$type };
                Object.keys(object).filter(function (d) { return d != "$type" })
                    .forEach(function (k) {
                        retVal[k] = object[k];
                        if (!retVal[k]);
                        else if (retVal[k].$type) // reorder k
                            retVal[k] = _reorderProperties(retVal[k]);
                        else if (Array.isArray(retVal[k]))
                            for (var i in retVal[k])
                                if (retVal[k][i].$type)
                                    retVal[k][i] = _reorderProperties(retVal[k][i]);
                    })
                return retVal;
            }
            return object;
        };

        /**
         * @summary Global error handler
         * @param {xhr} e The Errored request
         * @param {*} data 
         * @param {*} setting 
         * @param {*} err 
         */
        var _globalErrorHandler = function (data, setting, err) {
            if (data.status == 401 && data.getResponseHeader("WWW-Authenticate")) {
                if (_session && _session.exp > Date.now // User has a session that is valid, but is still 401 hmm... elevation!!!
                    && _elevator
                    && !_elevator.getToken() ||
                    _session == null && _elevator) {

                    // Was the response a security policy exception where the back end is asking for elevation on the same user account?
                    if (data.responseJSON &&
                        data.responseJSON.type == "SecurityPolicyException" &&
                        data.responseJSON.message == "error.elevate")
                        _elevator.elevate(_session);
                    else
                        _elevator.elevate(null);
                    return true;
                }
            }
            else
                console.warn(new Exception("Exception", "error.general", err, null));
            return false;
        };


        /**
            * @class ResourceWrapper
            * @memberof SanteDBWrapper
            * @constructor
            * @summary Represents a wrapper for a SanteDB resource
            * @param {any} _config The configuration object
            * @param {string} _config.resource The resource that is being wrapped
            * @param {APIWrapper} _config.api The API to use for this resource
            */
        function ResourceWrapper(_config) {

            /**
                * @method
                * @memberof SanteDBWrapper.ResourceWrapper
                * @summary Retrieves a specific instance of the resource this wrapper wraps
                * @param {string} id The unique identifier of the resource to retrieve
                * @param {any} state A unique state object which is passed back to the caller
                * @returns {Promise} The promise for the operation
                */
            this.getAsync = function (id, viewModel, state) {

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            };

            /**
                * @method
                * @memberof SanteDBWrapper.ResourceWrapper
                * @summary Queries for instances of the resource this wrapper wraps
                * @param {any} query The HDSI query to filter on
                * @param {any} state A unique state object which is passed back to the caller
                * @returns {Promise} The promise for the operation
                */
            this.findAsync = function (query, state) {

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });

            };

            /**
             * @method
             * @memberof SanteDBWrapper.ResourceWrapper
             * @param {any} query The query for the object that you are looking for
             * @summary Queries for instances of the resource this wrapper wraps in a synchronous fashion
             * @see {SanteDBWrapper.findAsync} For asynchronous method
             * @return {Promise} A promise which is blocked and not executed until the operation is complete
             */
            this.find = function (query) {

                // TODO: Use the C# bindings
                fullfill();
            }

            /**
                * @method
                * @memberof SanteDBWrapper.ResourceWrapper
                * @summary Inserts a specific instance of the wrapped resource
                * @param {any} data The data / resource which is to be created
                * @param {any} state A unique state object which is passed back to the caller
                * @returns {Promise} The promise for the operation
                */
            this.insertAsync = function (data, state) {

                if (data.$type !== _config.resource)
                    throw new Exception("ArgumentException", "error.invalidType", "Invalid type, resource wrapper expects " + _config.resource + " however " + data.$type + " specified");

                if (data.createdBy)
                    delete (data.createdBy);
                if (data.creationTime)
                    delete (data.creationTime);

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            };

            /**
             * @method
             * @memberof SantedBWrapper.ResourceWrapper
             * @summary Sends a patch to the service
             * @param {string} id The identifier of the object to patch
             * @param {string} etag The e-tag to assert
             * @param {Patch} patch The patch to be applied
             * @param {any} state A unique state object which is passed back to the caller
             * @returns {Promise} The promise for the operation
             */
            this.patchAsync = function (id, etag, patch, state) {
                if (patch.$type !== "Patch")
                    throw new Exception("ArgumentException", "error.invalidType", "Invalid type, resource wrapper expects Patch however " + data.$type + " specified");

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            }

            /**
                * @method
                * @memberof SanteDBWrapper.ResourceWrapper
                * @summary Updates the identified instance of the wrapped resource
                * @param {string} id The unique identifier for the object to be updated
                * @param {any} data The data / resource which is to be updated
                * @param {any} state A unique state object which is passed back to the caller
                * @returns {Promise} The promise for the operation
                */
            this.updateAsync = function (id, data, state) {

                if (data.$type !== _config.resource)
                    throw new Exception("ArgumentException", "error.invalidType", "Invalid type, resource wrapper expects " + _config.resource + " however " + data.$type + " specified");
                else if (data.id && data.id !== id)
                    throw new Exception("ArgumentException", "error.invalidValue", "Identifier mismatch, PUT identifier  " + id + " doesn't match " + data.id);

                if (data.updatedBy)
                    delete (data.updatedBy);
                if (data.updatedTime)
                    delete (data.updatedTime);

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            };

            /**
            * @method
            * @memberof SanteDBWrapper.ResourceWrapper
            * @summary Performs an obsolete (delete) operation on the server
            * @param {string} id The unique identifier for the object to be deleted
            * @param {any} state A unique state object which is passed back to the caller
            * @returns {Promise} The promise for the operation
            */
            this.deleteAsync = function (id, state) {

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });

            };

            /**
            * @method
            * @memberof SanteDBWrapper.ResourceWrapper
            * @summary Performs the specified LOCK operation on the server
            * @param {string} id The unique identifier for the object on which the invokation is to be called
            * @param {any} state A unique state object which is passed back to the caller
            * @returns {Promise} The promise for the operation
            */
            this.lockAsync = function (id, state) {

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            };

            /**
            * @method
            * @memberof SanteDBWrapper.ResourceWrapper
            * @summary Performs the specified UNLOCK operation on the server
            * @param {string} id The unique identifier for the object on which the invokation is to be called
            * @param {any} state A unique state object which is passed back to the caller
            * @returns {Promise} The promise for the operation
            */
            this.unLockAsync = function (id, state) {

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            };

            /**
                * @method
                * @memberof SanteDBWrapper.ResourceWrapper
                * @summary Performs a nullify on the specified object
                * @description A nullify differs from a delete in that a nullify marks an object as "never existed"
                * @param {string} id The unique identifier for the object to be nullified
                * @param {any} state A unique state object which is passed back to the caller
                * @returns {Promise} The promise for the operation
                */
            this.nullifyAsync = function (id, state) {

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            };

            /**
                * @method
                * @memberof SanteDBWrapper.ResourceWrapper
                * @summary Performs a cancel on the specified object
                * @description A cancel differs from a delete in that a cancel triggers a state change from NORMAL>CANCELLED
                * @param {string} id The unique identifier for the object to be cancelled
                * @param {any} state A unique state object which is passed back to the caller
                * @returns {Promise} The promise for the operation
                */
            this.cancelAsync = function (id, state) {
                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            };

            /**
             * @method
             * @memberof SanteDBWrapper.ResourceWrapper
             * @summary Performs a find operation on an association
             * @description Some resources allow you to chain queries which automatically scopes the results to the container
             * @param {string} id The identifier of the object whose children you want query 
             * @param {string} property The property path you would like to filter on 
             * @param {any} query The query you want to execute
             * @returns {Promise} A promise for when the request completes
             */
            this.findAssociatedAsync = function (id, property, query, state) {

                if (!id)
                    throw new Exception("ArgumentNullException", "Missing scoping identifier");
                else if (!property)
                    throw new Exception("ArgumentNullException", "Missing scoping property");
                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            };

            /**
             * @method
             * @memberof SanteDBWrapper.ResourceWrapper
             * @summary Adds a new association to the specified parent object
             * @param {string} id The identifier of the container
             * @param {string} property The associative property you want to add the value to
             * @param {any} data The data to be added as an associative object (note: Most resources require that this object already exist)
             * @param {any} state A stateful object for callback correlation
             * @returns {Promise} A promise which is fulfilled when the request is complete
             */
            this.addAssociatedAsync = function (id, property, data, state) {

                if (!id)
                    throw new Exception("ArgumentNullException", "Missing scoping identifier");
                else if (!property)
                    throw new Exception("ArgumentNullException", "Missing scoping property");

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            };

            /**
             * @method 
             * @memberof SanteDBWrapper.ResourceWrapper
             * @summary Removes an existing associated object from the specified scoper
             * @param {string} id The identifier of the container object
             * @param {string} property The property path from which the object is to be removed
             * @param {string} associatedId The identifier of the sub-object to be removed
             * @param {any} state A state for correlating multiple requests
             * @returns {Promise} A promise which is fulfilled when the request comletes
             */
            this.removeAssociatedAsync = function (id, property, associatedId, state) {
                if (!id)
                    throw new Exception("ArgumentNullException", "Missing scoping identifier");
                else if (!property)
                    throw new Exception("ArgumentNullException", "Missing scoping property");
                else if (!associatedId)
                    throw new Exception("ArgumentNullException", "Missing associated object id");

                return new Promise(function (fulfill, reject) {
                    // TODO: Use the C# bindings
                    fullfill();
                });
            }
        };

        // Public exposeing
        this.ResourceWrapper = ResourceWrapper;
        
        // Resources internal
        var _resources = {
            /**
                * @property {SanteDBWrapper.ResourceWrapper} bundle The bundle resource handler
                * @memberof SanteDBWrapper.resources
                * @summary Represents a resource wrapper that persists bundles
                */
            bundle: new ResourceWrapper({
                resource: "Bundle",
            }),
            /**
                * @property {SanteDB.ResourceWrapper}
                * @memberof SanteDBWrapper.resources
                * @summary Represents an resource wrapper that interoperates with the care planner
                */
            carePlan: new ResourceWrapper({
                resource: "CarePlan",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the Patient Resource
                */
            patient: new ResourceWrapper({
                resource: "Patient",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the SubstanceAdministration Resource
                */
            substanceAdministration: new ResourceWrapper({
                resource: "SubstanceAdministration",
            }),
            /**
                * @property {SanteDB.ResourceWrapper}
                * @memberof SanteDBWrapper.resources
                * @summary Represents the Act Resource
                */
            act: new ResourceWrapper({
                resource: "Act",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @summary Represents the entity resource
                * @memberof SanteDBWrapper.resources
                */
            entity: new ResourceWrapper({
                resource: "Entity",
            }),
            /**
             * @property {SanteDB.ResourceWrapper}
             * @summary A resource wrapper for Assigning Authorities
             * @memberof SanteDBWrapper.resources
             */
            assigningAuthority: new ResourceWrapper({
                resource: "AssigningAuthority",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @summary Represents the entity relationship resource
                * @memberof SanteDBWrapper.resources
                */
            entityRelationship: new ResourceWrapper({
                resource: "EntityRelationship",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the Observation Resource
                */
            observation: new ResourceWrapper({
                resource: "Observation",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the Place Resource
                */
            place: new ResourceWrapper({
                resource: "Place",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the Provider Resource
                */
            provider: new ResourceWrapper({
                resource: "Provider",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the UserEntity Resource
                */
            userEntity: new ResourceWrapper({
                resource: "UserEntity",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the Organization Resource
                */
            organization: new ResourceWrapper({
                resource: "Organization",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the Material Resource
                */
            material: new ResourceWrapper({
                resource: "Material",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the ManufacturedMaterial Resource
                */
            manufacturedMaterial: new ResourceWrapper({
                resource: "ManufacturedMaterial",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the ManufacturedMaterial Resource
                */
            concept: new ResourceWrapper({
                resource: "Concept",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the ConceptSet Resource
                */
            conceptSet: new ResourceWrapper({
                resource: "ConceptSet",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the ReferenceTerm Resource
                */
            referenceTerm: new ResourceWrapper({
                resource: "ReferenceTerm",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the CodeSystem Resource
                */
            codeSystem: new ResourceWrapper({
                resource: "CodeSystem",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the DeviceEntity Resource
                */
            deviceEntity: new ResourceWrapper({
                resource: "DeviceEntity",
            }),
            /**
                * @property {SanteDB.ResourceWrapper} 
                * @memberof SanteDBWrapper.resources
                * @summary Represents the ApplicationEntity Resource
                */
            applicationEntity: new ResourceWrapper({
                resource: "ApplicationEntity",
            }),
            /**
                * @property {SanteDB.ResourceWrapper}
                * @memberof SanteDBWrapper.resources
                * @summary Resource wrapper which interacts with the administrative task scheduler
                */
            task: new ResourceWrapper({
                resource: "Task",
            }),
            /**
                * @property {SanteDB.ResourceWrapper}
                * @memberof SanteDBWrapper.resources
                * @summary A resource wrapper for alerts which are messages between users
                */
            mail: new ResourceWrapper({
                resource: "MailMessage",
            }),
            /**
                * @property {SanteDB.ResourceWrapper}
                * @memberof SanteDBWrapper.resources
                * @summary A wrapper which is used for fetching user notifications
                **/
            tickle: new ResourceWrapper({
                resource: "Tickle",
            }),
            /**
             * @property {SanteDB.ResourceWrapper}
             * @memberOf SanteDBWrapper.resources
             * @summary Wrapper for Security USers
             */
            securityUser: new ResourceWrapper({
                resource: "SecurityUser",
            }),
            /**
             * @property {SanteDB.ResourceWrapper}
             * @memberOf SanteDBWrapper.resources
             * @summary Wrapper for Security Roles
             */
            securityRole: new ResourceWrapper({
                resource: "SecurityRole",
            }),
            /**
             * @property {SanteDB.ResourceWrapper}
             * @memberOf SanteDBWrapper.resources
             * @summary Wrapper for Security Devices
             */
            securityDevice: new ResourceWrapper({
                resource: "SecurityDevice",
            }),
            /**
             * @property {SanteDB.ResourceWrapper}
             * @memberOf SanteDBWrapper.resources
             * @summary Wrapper for Security Applications
             */
            securityApplication: new ResourceWrapper({
                resource: "SecurityApplication",
            }),
            /**
             * @property {SanteDB.ResourceWrapper}
             * @memberOf SanteDBWrapper.resources
             * @summary Wrapper for Security Policies
             */
            securityPolicy: new ResourceWrapper({
                resource: "SecurityPolicy",
            }),
            /**
             * @property {SanteDB.ResourceWrapper}
             * @memberOf SanteDBWrapper.resources
             * @summary Wrapper for provenance
             */
            securityProvenance: new ResourceWrapper({
                resource: "SecurityProvenance",
            }),
            /**
             * @property {SanteDB.ResourceWrapper}
             * @memberOf SanteDBWrapper.resources
             * @summary Wrapper for audit API
             */
            audit: new ResourceWrapper({
                resource: "AuditData",
            })
        };

        // HACK: Wrapper pointer facility = place
        _resources.facility = _resources.place;
        
       
        /**
            * @property
            * @memberof SanteDBWrapper
            * @summary Provides access to resource handlers
            */
        this.resources = _resources;
       
    };

if (!SanteDB)
    var SanteDB = new SanteDBWrapper();

/**
 * Return the string as a camel case
 * @param {String} str The String
 */
String.prototype.toCamelCase = function () {
    return this
        .replace(/\s(.)/g, function ($1) { return $1.toUpperCase(); })
        .replace(/\s/g, '')
        .replace(/^(.)/, function ($1) { return $1.toLowerCase(); });
}


/**
 * Pad the specified string
 * @param {String} str The String
 */
String.prototype.pad = function (pchar, len) {
    var pad = "";
    for (var i = 0; i < len; i++) pad += pchar;
    return (pad + this).slice(-len);
}