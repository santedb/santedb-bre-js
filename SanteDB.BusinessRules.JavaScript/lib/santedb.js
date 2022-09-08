/// <reference path="./santedb-model.js"/>
/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2022-5-30
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
            this.get = function (id, viewModel, state) {
                return SanteDBBre.get(__config.resource, id);
            };

            /**
                * @method
                * @memberof SanteDBWrapper.ResourceWrapper
                * @summary Queries for instances of the resource this wrapper wraps
                * @param {any} query The HDSI query to filter on
                * @param {any} state A unique state object which is passed back to the caller
                * @returns {Promise} The promise for the operation
                */
            this.find = function (query, state) {

                return SanteDBBre.find(_config.resource, query);

            };

            /**
                * @method
                * @memberof SanteDBWrapper.ResourceWrapper
                * @summary Inserts a specific instance of the wrapped resource
                * @param {any} data The data / resource which is to be created
                * @param {any} state A unique state object which is passed back to the caller
                * @returns {Promise} The promise for the operation
                */
            this.insert = function (data, state) {

                if (data.$type !== _config.resource)
                    throw new Exception("ArgumentException", "error.invalidType", "Invalid type, resource wrapper expects " + _config.resource + " however " + data.$type + " specified");

                if (data.createdBy)
                    delete (data.createdBy);
                if (data.creationTime)
                    delete (data.creationTime);

                return SanteDBBre.insert(data);
            };

            /**
                * @method
                * @memberof SanteDBWrapper.ResourceWrapper
                * @summary Updates the identified instance of the wrapped resource
                * @param {string} id The unique identifier for the object to be updated
                * @param {any} data The data / resource which is to be updated
                * @param {any} state A unique state object which is passed back to the caller
                * @returns {Promise} The promise for the operation
                */
            this.update = function (id, data, state) {

                if (data.$type !== _config.resource)
                    throw new Exception("ArgumentException", "error.invalidType", "Invalid type, resource wrapper expects " + _config.resource + " however " + data.$type + " specified");
                else if (data.id && data.id !== id)
                    throw new Exception("ArgumentException", "error.invalidValue", "Identifier mismatch, PUT identifier  " + id + " doesn't match " + data.id);

                if (data.updatedBy)
                    delete (data.updatedBy);
                if (data.updatedTime)
                    delete (data.updatedTime);

                return SanteDBBre.save(data);
            };

            /**
            * @method
            * @memberof SanteDBWrapper.ResourceWrapper
            * @summary Performs an obsolete (delete) operation on the server
            * @param {string} id The unique identifier for the object to be deleted
            * @param {any} state A unique state object which is passed back to the caller
            * @returns {Promise} The promise for the operation
            */
            this.delete = function (id, state) {

                return SanteDBBre.obsolete(__config.resource, id);

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
                var data = SanteDBBre.get(__config.resource, id);
                data.statusConcept = StatusKeys.Nullified;
                return SanteDBBre.save(data);
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
            this.cancel = function (id, state) {
                var data = SanteDBBre.get(__config.resource, id);
                data.statusConcept = StatusKeys.Cancelled;
                return SanteDBBre.save(data);
            };

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
                resource: "AuditEventData",
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