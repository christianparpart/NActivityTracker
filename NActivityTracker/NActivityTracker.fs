// Copyright 2018-2019 Fabulous contributors. See LICENSE.md for license.
namespace NActivityTracker

open System.Diagnostics
open Fabulous
open Fabulous.XamarinForms
open Fabulous.XamarinForms.LiveUpdate
open Xamarin.Forms
open System

module App = 
    type Activity = {
        Name : string
        Id : int
    }

    let getActivityById (id: int) (activities: Activity list) : Activity =
        match List.tryFind (fun (activity: Activity) -> activity.Id = id) activities with
        | Some activity -> activity
        | None -> activities.Head

    type ActivityLog = {
        Activity : int
        Start : DateTime
        End : DateTime
    }

    type Model = {
        StartedAt : DateTime        // timestamp when the last transition has been taken place
        CurrentActivityId : int     // current activity by activity Id
        NextFreeActivityId : int
        Activities : Activity list
        Log : ActivityLog list
    }

    type Msg = 
        | TimedTick
        | TransitionToActivity of int (* index to list view? or even better Activity.Id <- TODO *)

    let initModel = {
        CurrentActivityId = 1;
        NextFreeActivityId = 4;
        StartedAt = DateTime.Now;
        Activities = [
            { Name = "Idle"; Id = 1 };
            { Name = "Work"; Id = 2 };
            { Name = "Break from work"; Id = 3 }
        ];
        Log = []
    }

    let init () =
        let initialCommands = Cmd.batch [
            Cmd.ofMsg TimedTick
        ]
        initModel, initialCommands

    // updates the GUI with the time already spend in the currently active activity
    let timerUpdateCmd =
        async {
            do! Async.Sleep 1000
            return TimedTick
        } |> Cmd.ofAsyncMsg

    let update msg model =
        match msg with
        | TimedTick -> model, timerUpdateCmd
        | TransitionToActivity idx ->
            let newModel = { model with CurrentActivityId = idx; StartedAt = DateTime.Now }
            newModel, Cmd.none

    let createActivityTextCells (model: Model) =
        List.map (fun (activity: Activity) -> View.TextCell activity.Name) model.Activities

    let view (model: Model) (dispatch) =
        View.ContentPage(
          content = View.StackLayout(padding = Thickness 20.0, verticalOptions = LayoutOptions.Center,
            children = [ 
                View.Label(text = sprintf "%s: %s" ((getActivityById model.CurrentActivityId model.Activities).Name) ((DateTime.Now - model.StartedAt).ToString("d\.hh\:mm\:ss")),
                           horizontalOptions = LayoutOptions.Center,
                           fontSize = FontSize.fromNamedSize NamedSize.Title,
                           horizontalTextAlignment = TextAlignment.Center)
                View.ListView(
                    items = createActivityTextCells model,
                    // TODO: how to pre-select an item
                    itemSelected = (fun indexOption ->
                        match indexOption with
                        | Some index -> dispatch (TransitionToActivity model.Activities.[index].Id)
                        | None -> ()
                    )
                )
            ]))

    // Note, this declaration is needed if you enable LiveUpdate
    let program = XamarinFormsProgram.mkProgram init update view

type App () as app = 
    inherit Application ()

    let runner = 
        App.program
#if DEBUG
        |> Program.withConsoleTrace
#endif
        |> XamarinFormsProgram.run app

#if DEBUG
    // Uncomment this line to enable live update in debug mode. 
    // See https://fsprojects.github.io/Fabulous/Fabulous.XamarinForms/tools.html#live-update for further  instructions.
    //
    //do runner.EnableLiveUpdate()
#endif

    // Uncomment this code to save the application state to app.Properties using Newtonsoft.Json
    // See https://fsprojects.github.io/Fabulous/Fabulous.XamarinForms/models.html#saving-application-state for further  instructions.
#if APPSAVE
    let modelId = "model"
    override __.OnSleep() = 

        let json = Newtonsoft.Json.JsonConvert.SerializeObject(runner.CurrentModel)
        Console.WriteLine("OnSleep: saving model into app.Properties, json = {0}", json)

        app.Properties.[modelId] <- json

    override __.OnResume() = 
        Console.WriteLine "OnResume: checking for model in app.Properties"
        try 
            match app.Properties.TryGetValue modelId with
            | true, (:? string as json) -> 

                Console.WriteLine("OnResume: restoring model from app.Properties, json = {0}", json)
                let model = Newtonsoft.Json.JsonConvert.DeserializeObject<App.Model>(json)

                Console.WriteLine("OnResume: restoring model from app.Properties, model = {0}", (sprintf "%0A" model))
                runner.SetCurrentModel (model, Cmd.none)

            | _ -> ()
        with ex -> 
            App.program.onError("Error while restoring model found in app.Properties", ex)

    override this.OnStart() = 
        Console.WriteLine "OnStart: using same logic as OnResume()"
        this.OnResume()
#endif


