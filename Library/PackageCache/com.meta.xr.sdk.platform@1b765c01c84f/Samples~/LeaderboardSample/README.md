# Oculus Platform SDK Leaderboard Sample

Last updated: May 2025

## Intro

A sample to demonstrate how to use the platform leaderboard api.

The sample itself is quite simple, feel free to just enter play mode and click
on "Get Mock Entries" to see the simple leaderboard in action

Follow the steps below to setup the leaderboard to see real game data

## Setup steps

Please follow the steps here:
<https://developers.meta.com/horizon/documentation/unity/ps-setup/>

The documentation above might look very scary, but to run this sample it is
relatively simple

1. Setup the platform app on developer portal as detailed in the doc

2. In Unity, see up top, Meta -> Platform -> Edit Settings, fill in the settings
   page with the app id created for mobile meta quest

3. Make sure you have the appropriate data use checkup information filled in the
   portal. The ones below allows us to get the leaderboard data and the username
   associated with each entry

   - Under User ID, select "Use Leaderboards" and "View Oculus Username"
   - Under User Profile, select "Use Leaderboards" and "View Oculus Username"

4. Create a test user on developer portal and use standalone login in the
   settings page with test user credentials
   <https://developers.meta.com/horizon/resources/test-users/>

5. Now go over to the entitlement sample, run it, wait for a couple of seconds,
   if that works then you have successfully setup the platform app

6. In Developer Portal, under Engagement -> Leaderboards, create a new
   leaderboard. For API name, choose your own and modify the sample code
   ([LeaderboardSampleScript.cs](./LeaderboardSampleScript.cs)) or
   "sample_leaderboard_visible" defined in the code
   <https://developers.meta.com/horizon/documentation/unity/ps-leaderboards>

7. If everything works above you can just run the leaderboard sample, write
   score for your account, and click "Get Real Entries", now it should show up!
