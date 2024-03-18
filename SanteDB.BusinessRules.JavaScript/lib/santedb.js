/// <reference path="./santedb-model.js"/>
/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */

// Interactive SHIM between host environment and browser
var __SanteDBAppService = window.SanteDBAppService || {};
var jQuery = jQuery || undefined;
// Backing of execution environment
var ExecutionEnvironment = {
    Unknown: 0,
    Server: 1,
    Mobile: 2,
    Other: 3,
    Test: 4,
    Gateway: 5
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


//if (!SanteDB) 
/**
 * @class
 * @static
 * @constructor
 * @summary SanteDB Binding Class
 * @description This class exists as a simple interface which is implemented by host implementations of the SanteDB hostable core. This interface remains the same even though the 
 *              implementations of this file on each platform (Admin, BRE, Client, etc.) are different.
 * @property {SanteDBWrapper.ApplicationApi} application Functions for accessing the core application API
 * @property {SanteDBWrapper.ResourceApi} resources Functions for accessing resource APIs
 * @property {SanteDBWrapper.ConfigurationApi} configuration Functions for accessing application configuration
 * @property {SanteDBWrapper.AuthenticationApi} authentication Functions for authentication 
 * @property {SanteDBWrapper.LocalizationApi} localization Functions related to localization
    * @property {*} api Provides direct access to API instances
    * @property {SanteDBWrapper.APIWrapper} api.hdsi Reference to the configured Health Data Service Interface helper
    * @property {SanteDBWrapper.APIWrapper} api.ami Reference to the configured Administration Management Interface helper
 */
function SanteDBWrapper() {
    "use strict";

    var _viewModelJsonMime = "application/json+sdb-viewmodel";

    // Get the version of this API Wrapper
    this.getVersion = function () {
        return __SanteDBAppService.GetVersion();
    }

    /**
     * @public
     * @summary Convert an object to parameters
     * @param {any} object The JavaScript object to convert to a parameters
     * @return {Parameters} The parameters
     */
    this.convertToParameters = function (object) {
        return { parameter: Object.keys(object).map(k => { return { name: k, value: object[k] } }) };
    }

    /**
     * @public
     * @summary Convert parameters to an object
     * @param {Parameters} parms The parameters to convert
     * @return {any} The JavaScript object 
     */
    this.convertFromParameters = function (parms) {
        if (!parms.parameter) {
            return null;
        }

        var retVal = {};
        parms.parameter.forEach(p => retVal[p.name] = p.value);
        return retVal;
    }

    /**
     * @private
     * @summary Global error handler
     * @param {xhr} e The Errored request
     * @param {*} data 
     * @param {*} setting 
     * @param {*} err 
     */
    var _globalErrorHandler = function (data, setting, err) {
        if (data.status == 401 && data.getResponseHeader("WWW-Authenticate")) {
            if (_session &&
                _session.exp > Date.now() && // User has a session that is valid, but is still 401 hmm... elevation!!!
                _elevator &&
                !_elevator.getToken() ||
                (_session == null || !_session.access_token) && _elevator) {

                // Was the response a security policy exception where the back end is asking for elevation on the same user account?
                if (data.responseJSON &&
                    data.responseJSON.$type == "PolicyViolationException" &&
                    data.getResponseHeader("WWW-Authenticate").indexOf("insufficient_scope") > -1)
                    _elevator.elevate(angular.copy(_session), [data.responseJSON.policyId]);
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
     * @private
     * @summary Re-orders the JSON object properties so that $type appears as the first property
     * @param {any} object The object whose properites should be reordered
     * @returns {any} The appropriately ordered object
     */
    var _reorderProperties = function (object) {

        // Object has $type and $type is not the first property
        if (object.$type) {
            var retVal = { $type: object.$type };
            Object.keys(object).filter(function (d) { return d != "$type" && !d.endsWith("Model") })
                .forEach(function (k) {
                    retVal[k] = object[k];
                    if (!retVal[k] || k.startsWith("_"));
                    else if (retVal[k].$type) // reorder k
                        retVal[k] = _reorderProperties(retVal[k]);
                    else if (Array.isArray(retVal[k]))
                        for (var i in retVal[k])
                            if (retVal[k][i].$type)
                                retVal[k][i] = _reorderProperties(retVal[k][i]);
                });
            return retVal;
        }
        return object;
    };

    this._reorderProperties = _reorderProperties;
    this._globalErrorHandler = _globalErrorHandler;

    /**
     * @class
     * @constructor
     * @memberof SanteDBWrapper
     * @summary Represents a wrapper for various resources on the SanteDB API
     * @property {ResourceWrapper} bundle Functions for interacting with {@link Bundle}
     * @property {ResourceWrapper} act Functions for interacting with {@link Act}
     * @property {ResourceWrapper} applicationEntity Functions for interacting with  {@link ApplicationEntity}
     * @property {ResourceWrapper} identityDomain Functions for interacting with {@link identityDomain}
     * @property {ResourceWrapper} carePlan Functions for interacting with {@link CarePlan} 
     * @property {ResourceWrapper} codeSystem Functions for interacting with {@link CodeSystem}
     * @property {ResourceWrapper} concept Functions for interacting with {@link Concept}
     * @property {ResourceWrapper} conceptSet Functions for interacting with {@link ConceptSet}
     * @property {ResourceWrapper} configuration Functions for interacting with {@link Configuration}
     * @property {ResourceWrapper} deviceEntity Functions for interacting with {@link DeviceEntity}
     * @property {ResourceWrapper} entityRelationship Functions for interacting with {@link EntityRelationship}
     * @property {ResourceWrapper} locale Functions for interacting with {@link Locale}
     * @property {ResourceWrapper} mail Functions for interacting with {@link Mail}
     * @property {ResourceWrapper} manufacturedMaterial Functions for interacting with {@link ManufacturedMaterial}
     * @property {ResourceWrapper} material Functions for interacting with {@link Material}
     * @property {ResourceWrapper} observation Functions for interacting with {@link Observation}
     * @property {ResourceWrapper} organization Functions for interacting with {@link Organization}
     * @property {ResourceWrapper} patient Functions for interacting with {@link Patient}
     * @property {ResourceWrapper} entityRelationship Functions for interacting with {@link EntityRelationship}
     * @property {ResourceWrapper} entity Functions for interacting with {@link Entity}
     * @property {ResourceWrapper} place Functions for interacting with {@link Place}
     * @property {ResourceWrapper} provider Functions for interacting with {@link Provider}
     * @property {ResourceWrapper} queue Functions for interacting with {@link Queue}
     * @property {ResourceWrapper} referenceTerm Functions for interacting with {@link ReferenceTerm}
     * @property {ResourceWrapper} substanceAdministration Functions for interacting with {@link SubstanceAdministration}
     * @property {ResourceWrapper} task Functions for interacting with {@link Task}
     * @property {ResourceWrapper} tickle Functions for interacting with {@link Tickle}
     * @property {ResourceWrapper} pubSubSubscriptionDefinition Functions for interacting with {@link SubscriptionDefinition}
     * @property {ResourceWrapper} pubSubChannelDefinition Functions for interacting with {@link PubSubChannel}
     * @property {ResourceWrapper} userEntity Functions for interacting with {@link UserEntity}
     * @property {ResourceWrapper} extensionType Functions for interacting with {@link ExtensionType}
     * @property {ResourceWrapper} matchConfiguration Functions for interacting with {@link MatchConfiguration}
     * @property {ResourceWrapper} configuration Functions for interacting with {@link Configuration}
     * @property {ResourceWrapper} dispatcherQueue Functions for interacting with {@link DispatcherQueueInfo}
     * @property {ResourceWrapper} sessionInfo Functions for interacting with {@link SessionInfo}
     * @property {ResourceWrapper} probe Functions for interacting with {@link Probe}
     * @property {ResourceWrapper} queue Functions for interacting with system synchronization queues
     */
    function ResourceApi() {

        // Reference to this
        var _me = this;

        /**
         * @method
         * @summary Sometimes when a link is retrieved it is a base class - this function ensures that the @value is of type @desiredType if not it will fetch
         * @param {any} value The current value of the object which should be the desired type
         * @param {String} desiredType The type of resource that the value should be or else the resource is fetched
         * @returns {Promise} A promise for the fulfillment of the check
         */
        this.ensureTypeAsync = function (value, desiredType) {
            if (value == null || value.$type === undefined || value.$type === desiredType) {
                return value;
            }
            else if (value.id) {
                return _me[desiredType.toCamelCase()].getAsync(value.id);
            }
        }

        /**
        * @type {ResourceWrapper}
        * @memberof SanteDBWrapper.ResourceApi
        * @summary Represents a resource wrapper that persists bundles
        */
        this.bundle = new ResourceWrapper({
            resource: "Bundle"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents an resource wrapper that interoperates with the care planner
            */
        this.carePlan = new ResourceWrapper({
            resource: "CarePlan"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the Patient Resource
            */
        this.patient = new ResourceWrapper({
            resource: "Patient"
        });

        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the PubSubChannel Resource
            */
        this.pubSubChannelDefinition = new ResourceWrapper({
            resource: "PubSubChannelDefinition"
        });

        /**
        * @type {ResourceWrapper}
        * @memberof SanteDBWrapper.ResourceApi
        * @summary Represents the PubSubSubscription Resource
        */
        this.pubSubSubscriptionDefinition = new ResourceWrapper({
            resource: "PubSubSubscriptionDefinition"
        });

        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the Patient Resource
            */
        this.extensionType = new ResourceWrapper({
            resource: "ExtensionType"
        });

        /**
         * @type {ResourceWrapper}
         * @memberof SanteDBWrapper.ResourceApi
         * @summary Match configuration API
         */
        this.matchConfiguration = new ResourceWrapper({
            resource: "MatchConfiguration"
        });

        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the SubstanceAdministration Resource
            */
        this.substanceAdministration = new ResourceWrapper({
            resource: "SubstanceAdministration"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the Act Resource
            */
        this.act = new ResourceWrapper({
            resource: "Act"
        });
        /**
            * @type {ResourceWrapper}
            * @summary Represents the entity resource
            * @memberof SanteDBWrapper.ResourceApi
            */
        this.entity = new ResourceWrapper({
            resource: "Entity"
        });
        /**
         * @type {ResourceWrapper}
         * @summary A resource wrapper for Assigning Authorities
         * @memberof SanteDBWrapper.ResourceApi
         */
        this.identityDomain = new ResourceWrapper({
            resource: "IdentityDomain"
        });
        /**
            * @type {ResourceWrapper}
            * @summary Represents the entity relationship resource
            * @memberof SanteDBWrapper.ResourceApi
            */
        this.entityRelationship = new ResourceWrapper({
            resource: "EntityRelationship"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the Observation Resource
            */
        this.observation = new ResourceWrapper({
            resource: "Observation"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the Place Resource
            */
        this.place = new ResourceWrapper({
            resource: "Place"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the Provider Resource
            */
        this.provider = new ResourceWrapper({
            resource: "Provider"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the UserEntity Resource
            */
        this.userEntity = new ResourceWrapper({
            resource: "UserEntity"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the Organization Resource
            */
        this.organization = new ResourceWrapper({
            resource: "Organization"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the Material Resource
            */
        this.material = new ResourceWrapper({
            resource: "Material"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the ManufacturedMaterial Resource
            */
        this.manufacturedMaterial = new ResourceWrapper({
            resource: "ManufacturedMaterial"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the ManufacturedMaterial Resource
            */
        this.concept = new ResourceWrapper({
            resource: "Concept"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the ConceptSet Resource
            */
        this.conceptSet = new ResourceWrapper({
            resource: "ConceptSet"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the ReferenceTerm Resource
            */
        this.referenceTerm = new ResourceWrapper({
            resource: "ReferenceTerm"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the CodeSystem Resource
            */
        this.codeSystem = new ResourceWrapper({
            resource: "CodeSystem"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the DeviceEntity Resource
            */
        this.deviceEntity = new ResourceWrapper({
            resource: "DeviceEntity"
        });
        /**
           * @type {ResourceWrapper}
           * @memberof SanteDBWrapper.ResourceApi
           * @summary Represents the UserEntity Resource
           */
        this.userEntity = new ResourceWrapper({
            resource: "UserEntity"
        });
        /**
         * @type {ResourceWrapper}
         * @memberof SanteDBWrapper.ResourceApi
         * @summary Represents the Person Resource
         */
        this.person = new ResourceWrapper({
            resource: "Person"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Represents the ApplicationEntity Resource
            */
        this.applicationEntity = new ResourceWrapper({
            resource: "ApplicationEntity"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Gets the configuration resource
            */
        this.configuration = new ResourceWrapper({
            resource: "Configuration"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Gets the queue control resource
            */
        this.queue = new ResourceWrapper({
            resource: "Queue"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary Resource wrapper which interacts with the administrative task scheduler
            */
        this.task = new ResourceWrapper({
            resource: "Task"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary A resource wrapper for alerts which are messages between users
            */
        this.mail = new ResourceWrapper({
            resource: "Mailbox"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary A wrapper which is used for fetching user notifications
            **/
        this.tickle = new ResourceWrapper({
            resource: "Tickle"
        });
        /**
            * @type {ResourceWrapper}
            * @memberof SanteDBWrapper.ResourceApi
            * @summary A wrapper for locale information which comes from the server
            */
        this.locale = new ResourceWrapper({
            resource: "Locale"
        });
        /**
         * @type {ResourceWrapper}
         * @memberOf SanteDBWrapper.resources
         * @summary Wrapper for Security USers
         */
        this.securityUser = new ResourceWrapper({
            resource: "SecurityUser"
        });
        /**
         * @type {ResourceWrapper}
         * @memberOf SanteDBWrapper.resources
         * @summary Wrapper for Security Roles
         */
        this.securityRole = new ResourceWrapper({
            resource: "SecurityRole"
        });
        /**
        * @type {ResourceWrapper}
        * @memberOf SanteDBWrapper.resources
        * @summary Wrapper for session information
        */
        this.sessionInfo = new ResourceWrapper({
            resource: "SessionInfo"
        });
        /**
         * @type {ResourceWrapper}
         * @memberOf SanteDBWrapper.resources
         * @summary Wrapper for Security Devices
         */
        this.securityDevice = new ResourceWrapper({
            resource: "SecurityDevice"
        });
        /**
         * @type {ResourceWrapper}
         * @memberOf SanteDBWrapper.resources
         * @summary Wrapper for Security Applications
         */
        this.securityApplication = new ResourceWrapper({
            resource: "SecurityApplication"
        });
        /**
         * @type {ResourceWrapper}
         * @memberOf SanteDBWrapper.resources
         * @summary Wrapper for Security Policies
         */
        this.securityPolicy = new ResourceWrapper({
            resource: "SecurityPolicy"
        });
        /**
         * @type {ResourceWrapper}
         * @memberOf SanteDBWrapper.resources
         * @summary Wrapper for Security Challenges
         */
        this.securityChallenge = new ResourceWrapper({
            resource: "SecurityChallenge"
        });
        /**
         * @type {ResourceWrapper}
         * @memberOf SanteDBWrapper.resources
         * @summary Wrapper for provenance
         */
        this.securityProvenance = new ResourceWrapper({
            resource: "SecurityProvenance"
        });
        /**
         * @type {ResourceWrapper}
         * @memberOf SanteDBWrapper.resources
         * @summary Wrapper for audit API
         */
        this.audit = new ResourceWrapper({
            resource: "Audit"
        });
        /**
         * @type {ResourceWrapper}
         * @memberOf SanteDBWrapper.resources
         * @summary Wrapper for subscription definition API
         */
        this.subscriptionDefinition = new ResourceWrapper({
            resource: "SubscriptionDefinition"
        });
        /**
         * @type {ResourceWrapper}
         * @memberOf SanteDBWrapper.resources
         * @summary Wrapper for subscription definition API
         */
        this.jobInfo = new ResourceWrapper({
            resource: "JobInfo"
        });
        /**
        * @type {ResourceWrapper}
        * @memberOf SanteDBWrapper.resources
        * @summary Wrapper for templates definition API
        */
        this.template = new ResourceWrapper({
            resource: "Template"
        });
        /**
        * @type {ResourceWrapper}
        * @memberOf SanteDBWrapper.resources
        * @summary Wrapper for certificates definition API
        */
        this.certificates = new ResourceWrapper({
            resource: "Certificate"
        });

        /**
        * @type {ResourceWrapper}
        * @memberOf SanteDBWrapper.resources
        * @summary Wrapper for alien data
        */
        this.foreignData = new ResourceWrapper({
            resource: "ForeignData"
        });

        /**
       * @type {ResourceWrapper}
       * @memberOf SanteDBWrapper.resources
       * @summary Wrapper for alien data mappings
       */
        this.foreignDataMap = new ResourceWrapper({
            resource: "ForeignDataMap"
        });
    };

    // HACK: Wrapper pointer facility = place
    var _resources = new ResourceApi();
    _resources.facility = _resources.place;

    // Provides localization support functions
    var _localeCache = {};
    /**
     * @class
     * @constructor
     * @protected
     * @memberof SanteDBWrapper
     * @summary Functions related to the localization of santedb
     */
    function LocalizationApi() {
        /**
         * @summary Default date formats
         * @enum
         * @memberof SanteDBWrapper.LocalizationApi
         * @property {string} year The format of year precision dates
         * @property {string} month The format of month precision dates
         * @property {string} day The format of day precision dates
         * @property {string} hour The format of hour precision dates
         * @property {string} minute The format of minute precision dates
         * @property {string} second The format of second precision dates
         */
        this.dateFormats = {
            year: 'YYYY',
            month: 'YYYY-MM',
            day: 'YYYY-MM-DD',
            hour: 'YYYY-MM-DD HH',
            minute: 'YYYY-MM-DD HH:mm',
            second: 'YYYY-MM-DD HH:mm:ss'
        };


        /**
            * @summary Gets a string from the current user interface localization
            * @memberof SanteDBWrapper.LocalizationApi
            * @method getString
            * @param {string} stringId The id of the localization string to get
            * @param {any} parameters The parameters used to substitute the string value
            * @returns {string} The localized string
            */
        this.getString = function (stringId, parameters) {
            try {
                var retVal = SanteDBBre.GetString(stringId);

                if (retVal) {
                    retVal = retVal.replace(/\{.*?\}/ig, function (s) {
                        if (typeof s === 'string' && parameters) {
                            return parameters[s.substring(1, s.length - 1)];
                        }
                        else {
                            return s;
                        }
                    });
                }
                return retVal || stringId;
            }
            catch (e) {
                console.error(e);
                return stringId;
            }
        }
       

    }

    
    /**
     * @memberof SanteDBWrapper
     * @summary Provide access to localization data
        * @public
     * @type {LocalizationApi}
     */
    this.locale = new LocalizationApi();
    /**
        * @type {ResourceApi}
        * @memberof SanteDBWrapper
        * @summary Provides access to resource handlers
        * @public
        */
    this.resources = _resources;
    
};

/**
 * @type {SanteDBWrapper}
 * @global
 */
var SanteDB = new SanteDBWrapper();

/**
 * @enum {CertificateStoreName}
 * @memberof SanteDB
 * @summary Certificate store names
 */
SanteDB.CertificateStoreName = {
    ServiceUser: "CurrentUser",
    EntireMachine: "LocalMachine"
};
