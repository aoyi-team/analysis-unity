using System;
using UnityEngine;

public static class CharacterFactory
{
    public static BasePlayerLogic CreatePlayerLogic(int heroId)
    {
        string className = $"Hero_{heroId}_Logic";
        Type type =Type.GetType(className);
        if(type != null &&typeof(BasePlayerLogic).IsAssignableFrom(type))
        {
            return Activator.CreateInstance(type) as BasePlayerLogic;
        }
        Debug.LogError($"´´½¨½ÇÉ«Âß¼­Æ÷Ê§°Ü!½ÇÉ«ºÅ:{heroId}");
        return null;
    }

    public static BasePlayerView CreatePlayerView(int heroID,ref GameObject o)
    {
        string className= $"Hero_{heroID}_View";
        Type type=Type.GetType(className);
        if(type!=null&&typeof(BasePlayerView).IsAssignableFrom(type))
        {
            BasePlayerView view = o.AddComponent(type) as BasePlayerView;
            return view;
        }
        Debug.LogError($"´´½¨½ÇÉ«äÖÈ¾Æ÷Ê§°Ü!½ÇÉ«ºÅ:{heroID}");
        return null;
    }
}