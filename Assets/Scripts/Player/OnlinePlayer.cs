using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Proyecto26;
using RSG;
using UniRx.Async;
using UnityEngine;

public class OnlinePlayer
{
    public Profile LastProfile { get; private set; }
    
    public bool IsAuthenticated { get; private set; }
    
    public bool IsAuthenticating { get; private set; }

    public Promise<Profile> Authenticate(string password)
    {
        IsAuthenticating = true;
        
        return new Promise<Profile>((resolve, reject) =>
        {
            RestClient.Post<Session>(new RequestHelper
            {
                Uri = Context.Host + "/session",
                BodyString = JObject.FromObject(new
                {
                    username = GetUid(),
                    password,
                    token = SecuredConstants.AuthenticationVerificationToken
                }).ToString()
            }).Then(session =>
                {
                    SetJwtToken(session.token);
                    Debug.Log(session.token);
                    return RestClient.Get<Profile>($"{Context.Host}/profile/{session.user.uid}/full");
                }
            ).Then(profile =>
            {
                IsAuthenticated = true;
                LastProfile = profile;
                resolve(profile);
            }).Catch(result =>
            {
                SetJwtToken(null);
                reject(result);
            }).Finally(() => IsAuthenticating = false);
        });
    }

    public Promise<Profile> AuthenticateWithJwtToken()
    {
        IsAuthenticating = true;

        var jwtToken = GetJwtToken();
        if (jwtToken == null) throw new ArgumentException();

        return new Promise<Profile>((resolve, reject) =>
        {
            RestClient.Post<Session>(new RequestHelper
            {
                Uri = Context.Host + "/session",
                BodyString = JObject.FromObject(new
                {
                    token = SecuredConstants.AuthenticationVerificationToken
                }).ToString(),
                Headers = new Dictionary<string, string>
                {
                    {"Authorization", "JWT " + jwtToken}
                }
            }).Then(session =>
            {
                SetJwtToken(session.token);
                Debug.Log(session.token);
                return RestClient.Get<Profile>($"{Context.Host}/profile/{session.user.uid}/full");
            }
            ).Then(profile =>
            {
                IsAuthenticated = true;
                LastProfile = profile;
                resolve(profile);
            }).Catch(result =>
            {
                SetJwtToken(null);
                reject(result);
            }).Finally(() => IsAuthenticating = false);
        });
    }

    public void Deauthenticate()
    {
        SetJwtToken(null);
        LastProfile = null;
        IsAuthenticating = false;
        IsAuthenticated = false;
    }
    
    public string GetUid()
    {
        return PlayerPrefs.GetString("last_username");
    }

    public void SetUid(string uid)
    {
        PlayerPrefs.SetString("last_username", uid);
    }

    public string GetJwtToken()
    {
        return SecuredPlayerPrefs.GetString("JwtToken", null);
    }

    public void SetJwtToken(string token)
    {
        SecuredPlayerPrefs.SetString("JwtToken", token);
    }
}