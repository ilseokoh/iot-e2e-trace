
/*global log*/
/*global updateState*/
/*global updateProperty*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Default properties
var properties = {
    Type: "Rebutton",
    ResetTime: "",
    TotalCount: 0
};

/**
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 * @param previousProperties  The device properties since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState, previousProperties) {

    var state = {
        hit: 1,
        hitTime: "",
        corellationId: ""
    };

    // just increse the hit count
    state.hit = 0;
    state.hitTime = context.currentTime;

    // reset total count
    properties.TotalCount = 0;
    properties.ResetTime = context.currentTime;
    
    //updateState(state);
    //updateProperty("ResetTime", properties.ResetTime);
    //updateProperty("TotalCount", properties.TotalCount);
}