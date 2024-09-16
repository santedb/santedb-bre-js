/// <reference path="../.ref/js/santedb-bre.js" />
/// <reference path="../.ref/js/santedb-model.js" />
/// <reference path="../.ref/js/santedb.js" />
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
 */

/**
 * Demoland Identifier Generator for IIS ID
 * --
 * Instead of using UUIDs which are difficult for people to convey - we generate a 12 digit identifier with a checkdigit
 */

function generateUhid() {

    var source = SanteDBBre.NewGuid().toByteArray().reduce(function(a,b) { return ((a*b) + 1) % 9999999999; });
    var seed = ("0" + source).split('').map(function (a) { return parseInt(a); }).reduce(function(a,b) { return ((a + b) * 10) % 97; });
    seed *= 10; seed %= 97;
    var checkDigit = (97 - seed + 1) % 97;
    checkDigit += "";

    var sourceStr = "" + source;
    return { value: sourceStr.substring(0, 4) + '-' + sourceStr.substring(4, 8) + '-' + sourceStr.substring(8, 10) + checkDigit.pad('0', 2) };
};

/**
 * Business rule - when incoming patient master has been persisted not in MDM mode
 */
function appendPatientID(patient) {

    if (!patient.identifier)
        patient.identifier = {};

    // Don't assign dead people identifiers
    if(patient.deceasedDate)
        return;
    
    // If operating in a server environment
    if (!patient.identifier.DL_MHMS_HIS_IIS_ID)
        patient.identifier.DL_MHMS_HIS_IIS_ID = generateUhid();
   
    return patient;
};

// Bind the business rules
// ROT gets the new UHID
SanteDBBre.AddBusinessRule("dl.iis.identifier", "Patient", "BeforeInsert", { "deceasedDate": "null", "typeConcept": "479896B0-35D5-4842-8109-5FDBEE14E8A4" }, appendPatientID);

// Add identifier generators
if(SanteDB.application) {
    SanteDB.application.addIdentifierGenerator("DL_MHMS_HIS_IIS_ID", generateUhid);
}