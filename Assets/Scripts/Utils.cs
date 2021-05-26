using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Utils {

    public enum SolarizedColors {
        brblack, brwhite, bryellow, brred, brmagenta, brblue, brcyan, brgreen,
        black, white, yellow, red, magenta, blue, cyan, green,
    }

    public static Dictionary<SolarizedColors, Color> solColorCodes = new Dictionary<SolarizedColors, Color>() {
        {SolarizedColors.brblack, new Color(0,43,54)},
        {SolarizedColors.brwhite, new Color(253,246,227)},
        {SolarizedColors.bryellow, new Color(101,123,131)},
        {SolarizedColors.brred, new Color(203,75,22)},
        {SolarizedColors.brmagenta, new Color(108,113,196)},
        {SolarizedColors.brblue, new Color(131,148,150)},
        {SolarizedColors.brcyan, new Color(147,161,161)},
        {SolarizedColors.brgreen, new Color(88,110,117)},
        {SolarizedColors.black, new Color(7,54,66)},
        {SolarizedColors.white, new Color(238,232,213)},
        {SolarizedColors.yellow, new Color(181,137,0)},
        {SolarizedColors.red, new Color(211,1,2)},
        {SolarizedColors.magenta, new Color(211,54,130)},
        {SolarizedColors.blue, new Color(38,139,210)},
        {SolarizedColors.cyan, new Color(42,161,152)},
        {SolarizedColors.green, new Color(133,153,0)},
    };

    public static T getRandomElement<T>(this IEnumerable<T> list) {
        var rnd = new System.Random();
        return list.OrderBy(i => rnd.Next()).First();
    }

    public static List<T> getManyRandomElements<T>(this IEnumerable<T> list, int number) {
        var rnd = new System.Random();
        return list.OrderBy(i => rnd.Next()).Take(number).ToList();
    }

    public static void assignSpriteFromPath(this GameObject gameObj, string path) {
        gameObj.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(path);
    }
}