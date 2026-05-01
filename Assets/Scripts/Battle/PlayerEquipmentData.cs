using System.Collections.Generic;
using System.Linq;

public static class PlayerEquipmentData
{
    private static List<Item> equippedEquipments = new List<Item>();
    private static List<Item> equippedArtifacts = new List<Item>();

    public static void SetEquippedEquipments(List<Item> items) => equippedEquipments = items.ToList();
    public static void SetEquippedArtifacts(List<Item> items) => equippedArtifacts = items.ToList();

    public static List<Item> GetEquippedEquipments() => equippedEquipments.ToList();
    public static List<Item> GetEquippedArtifacts() => equippedArtifacts.ToList();
}