/*
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2019-11-27
 */
SanteDBBre.AddBusinessRule("Patient", "AfterInsert", {}, function (patient) {
    // Simplify
    //var simplePatient = new OpenIZModel.Patient(patient);
    var simplePatient = patient;
    // Should get service
    var serviceManager = SanteDBBre.GetService("IServiceManager");
    console.assert(serviceManager != null, "Missing Service Manager");
    console.assert(serviceManager.AddServiceProvider !== undefined, "Service Manager isn't really really a service manager");
    console.assert(simplePatient != null, "Patient is null");
    console.assert(simplePatient.genderConceptModel != null, "Gender is null");
    console.assert(simplePatient.genderConceptModel.mnemonic == "Female", "Expected Female");
    console.assert(simplePatient.participation != null, "Participation missing");
    console.assert(simplePatient.participation.RecordTarget[0] != null, "Record Target missing");
    console.assert(simplePatient.name != null, "Names null");
    console.assert(simplePatient.name.Legal != null, "Names missing Legal");
    console.assert(simplePatient.name.Legal[0].component != null, "Name missing components");
    console.assert(simplePatient.name.Legal[0].component.Given[0] == "James", "Expected James as given name");
    console.assert(simplePatient.name.Legal[0].component.Family[0] == "Smith", "Expected Smith as family");
    simplePatient.dateOfBirth = new Date();

    var results = SanteDB.resources.concept.find(
        { mnemonic: "Female" }
    );
    simplePatient.genderConceptModel = results.resource[0];
    
    return simplePatient;
});

/** 
 * Sample Validator for Patient - 
 * Must have gender, must be present
 */
SanteDBBre.AddValidator("Patient", function (patient) {

    var retVal = new Array();

    if (patient == null)
        retVal.push({ text: "NullValue", priority: 1 });
    else if (patient.genderConcept == null)
        retVal.push({ text: "NoGender", priority: 1 });

    return retVal;
});