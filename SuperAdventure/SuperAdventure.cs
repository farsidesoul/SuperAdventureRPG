﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using Engine;

namespace SuperAdventure
{
    public partial class SuperAdventure : Form
    {
        private Player _player;
        private Monster _currentMonster;

        public SuperAdventure()
        {
            InitializeComponent();

            _player = new Player(10, 10, 20, 0, 1);
            MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
            _player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_RUSTY_SWORD), 1));
 
            UpdatePlayerStats();
        }

        private void btnNorth_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToNorth);
        }

        private void btnWest_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToWest);
        }

        private void btnEast_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToEast);
        }

        private void btnSouth_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToSouth);
        }

        private void MoveTo(Location newLocation)
        {
            // Does the location have any required items?
            if (!_player.HasRequiredItemToEnterThisLocation(newLocation))
            {
                rtbMessages.Text += @"You must have a " + newLocation.ItemRequiredToEnter.Name +
                                    @" to enter this location." + Environment.NewLine;
                return;
            }

            // Update the player's current location
            _player.CurrentLocation = newLocation;

            // Show/hide available movement locations
            btnNorth.Visible = (newLocation.LocationToNorth != null);
            btnEast.Visible = (newLocation.LocationToEast != null);
            btnSouth.Visible = (newLocation.LocationToSouth != null);
            btnWest.Visible = (newLocation.LocationToWest != null);

            // Display current location name and description
            rtbLocation.Text = newLocation.Name + Environment.NewLine;
            rtbLocation.Text += newLocation.Description + Environment.NewLine;

            // Completely heal the player
            _player.CurrentHitPoints = _player.MaximumHitPoints;

            // Update Hit points in UI
            UpdatePlayerStats();

            // Does the location have a quest?
            if (newLocation.QuestAvailableHere != null)
            {
                // See if player has the quest or has already completed it
                bool playerAlreadyHasQuest = _player.HasThisQuest(newLocation.QuestAvailableHere);
                bool playerAlreadyCompletedQuest = _player.CompletedThisQuest(newLocation.QuestAvailableHere);

                // See if player already has the quest
                if (playerAlreadyHasQuest)
                {
                    // If player has not completed quest yet
                    if (!playerAlreadyCompletedQuest)
                    {
                        // See if the player has all the items needed to complete the quest
                        bool playerHasAllItemsToCompleteQuest =
                            _player.HasAllQuestCompletionItems(newLocation.QuestAvailableHere);
                        
                        // The player has all items required to complete the quest
                        if (playerHasAllItemsToCompleteQuest)
                        {
                            // Display Message
                            rtbMessages.Text += Environment.NewLine;
                            rtbMessages.Text += @"You complete the '" + newLocation.QuestAvailableHere.Name +
                                                @"' quest." +
                                                Environment.NewLine;

                            // Remove quest items from the inventory
                            _player.RemoveQuestCompletionItems(newLocation.QuestAvailableHere);

                            // Give quest rewards
                            rtbMessages.Text += @"You recieve: " + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardExperiencePoints.ToString() +
                                                @" experience points." + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardGold.ToString() + @" gold." +
                                                Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardItem.Name + Environment.NewLine;
                            rtbMessages.Text += Environment.NewLine;

                            _player.ExperiencePoints += newLocation.QuestAvailableHere.RewardExperiencePoints;
                            _player.Gold += newLocation.QuestAvailableHere.RewardGold;

                            // Add the reward item to players inventory
                            _player.AddItemToInventory(newLocation.QuestAvailableHere.RewardItem);
                            
                            // Mark quest as complete
                            _player.MarkQuestCompleted(newLocation.QuestAvailableHere);
                        }
                    }
                }

                else
                {
                    // The player does not already have the quest
                    // Display the messages
                    rtbMessages.Text += @"You recieve the '" + newLocation.QuestAvailableHere.Name + @"' quest." +
                                        Environment.NewLine;
                    rtbMessages.Text += newLocation.QuestAvailableHere.Description + Environment.NewLine;
                    rtbMessages.Text += @"To complete it, return with: " + Environment.NewLine;
                    foreach (QuestCompletionItem qci in newLocation.QuestAvailableHere.QuestCompletionItems)
                    {
                        if (qci.Quantity == 1)
                        {
                            rtbMessages.Text += qci.Quantity.ToString() + " " + qci.Details.Name + Environment.NewLine;
                        }
                        else
                        {
                            rtbMessages.Text += qci.Quantity.ToString() + " " + qci.Details.NamePlural +
                                                Environment.NewLine;
                        }
                    }
                    rtbMessages.Text += Environment.NewLine;

                    // Add the quest to the player's quest list
                    _player.Quests.Add(new PlayerQuest(newLocation.QuestAvailableHere));
                }
            }

            // Does the location have a monster?
            if (newLocation.MonsterLivingHere != null)
            {
                rtbMessages.Text += @"You see a " + newLocation.MonsterLivingHere.Name + Environment.NewLine;

                // Make a new monster
                Monster standardMonster = World.MonsterByID(newLocation.MonsterLivingHere.ID);

                _currentMonster = new Monster(standardMonster.ID, standardMonster.Name, standardMonster.MaximumDamage,
                    standardMonster.RewardExperiencePoints, standardMonster.RewardGold,
                    standardMonster.CurrentHitPoints, standardMonster.MaximumHitPoints);

                foreach (LootItem lootItem in standardMonster.LootTable)
                {
                    _currentMonster.LootTable.Add(lootItem);
                }

                cboWeapons.Visible = true;
                cboPotions.Visible = true;
                btnUseWeapon.Visible = true;
                btnUsePotion.Visible = true;
            }
            else
            {
                _currentMonster = null;

                cboWeapons.Visible = false;
                cboPotions.Visible = false;
                btnUseWeapon.Visible = false;
                btnUsePotion.Visible = false;
            }

            // Refresh player's inventory list
            UpdateInventoryListInUI();

            // Refresh player's quest list
            UpdateQuestListInUI();

            // Refresh player's weapons combobox
            UpdateWeaponListInUI();

            // Refresh player's potions combobox
            UpdatePotionListInUI();
            
        }

    private void UpdateInventoryListInUI()
    {
        dgvInventory.RowHeadersVisible = false;
 
        dgvInventory.ColumnCount = 2;
        dgvInventory.Columns[0].Name = "Name";
        dgvInventory.Columns[0].Width = 197;
        dgvInventory.Columns[1].Name = "Quantity";
 
        dgvInventory.Rows.Clear();
 
        foreach(InventoryItem inventoryItem in _player.Inventory)
        {
            if(inventoryItem.Quantity > 0)
            {
                dgvInventory.Rows.Add(new[] { inventoryItem.Details.Name, inventoryItem.Quantity.ToString() });
            }
        }
    }
 
    private void UpdateQuestListInUI()
    {
        dgvQuests.RowHeadersVisible = false;
 
        dgvQuests.ColumnCount = 2;
        dgvQuests.Columns[0].Name = "Name";
        dgvQuests.Columns[0].Width = 197;
        dgvQuests.Columns[1].Name = "Done?";
 
        dgvQuests.Rows.Clear();
 
        foreach(PlayerQuest playerQuest in _player.Quests)
        {
            dgvQuests.Rows.Add(new[] { playerQuest.Details.Name, playerQuest.IsCompleted.ToString() });
        }
    }
 
    private void UpdateWeaponListInUI()
    {
        List<Weapon> weapons = new List<Weapon>();
 
        foreach(InventoryItem inventoryItem in _player.Inventory)
        {
            if(inventoryItem.Details is Weapon)
            {
                if(inventoryItem.Quantity > 0)
                {
                    weapons.Add((Weapon)inventoryItem.Details);
                }
            }
        }
 
        if(weapons.Count == 0)
        {
            // The player doesn't have any weapons, so hide the weapon combobox and "Use" button
            cboWeapons.Visible = false;
            btnUseWeapon.Visible = false;
        }
        else
        {
            cboWeapons.DataSource = weapons;
            cboWeapons.DisplayMember = "Name";
            cboWeapons.ValueMember = "ID";
 
            cboWeapons.SelectedIndex = 0;
        }
    }
 
    private void UpdatePotionListInUI()
    {
        List<HealingPotion> healingPotions = new List<HealingPotion>();
 
        foreach(InventoryItem inventoryItem in _player.Inventory)
        {
            if(inventoryItem.Details is HealingPotion)
            {
                if(inventoryItem.Quantity > 0)
                {
                    healingPotions.Add((HealingPotion)inventoryItem.Details);
                }
            }
        }
 
        if(healingPotions.Count == 0)
        {
            // The player doesn't have any potions, so hide the potion combobox and "Use" button
            cboPotions.Visible = false;
            btnUsePotion.Visible = false;
        }
        else
        {
            cboPotions.DataSource = healingPotions;
            cboPotions.DisplayMember = "Name";
            cboPotions.ValueMember = "ID";
 
            cboPotions.SelectedIndex = 0;
        }
    }
        private void btnUseWeapon_Click(object sender, EventArgs e)
        {
            // Get the currently selected weapon
            Weapon currentWeapon = (Weapon) cboWeapons.SelectedItem;

            // determine the amount of damage to do to the monster
            int damagetoMonster = RandomNumberGenerator.NumberBetween(currentWeapon.MinimumDamage,
                currentWeapon.MaximumDamage);

            // Apply the damage to the monsters current hitpoints
            _currentMonster.CurrentHitPoints -= damagetoMonster;

            // Display message
            rtbMessages.Text += @"You hit the " + _currentMonster.Name + @" for " + damagetoMonster +
                                @" points." + Environment.NewLine;

            // Check if monster is dead
            if (_currentMonster.CurrentHitPoints <= 0)
            {
                // Monster is dead
                rtbMessages.Text += Environment.NewLine;
                rtbMessages.Text += @"You defeated the " + _currentMonster.Name + Environment.NewLine;

                // give player experience points for killing the monster
                _player.ExperiencePoints += _currentMonster.RewardExperiencePoints;
                rtbMessages.Text += @"You recieve " + _currentMonster.RewardExperiencePoints + @" experience points." +
                                    Environment.NewLine;

                // give player gold for killing monster
                _player.Gold += _currentMonster.RewardGold;
                rtbMessages.Text += @"You recieve " + _currentMonster.RewardGold + @" gold." + Environment.NewLine;

                // Check if player levels up
                DoesPlayerLevelUp();

                // Get random loot items from the monster
                List<InventoryItem> lootedItems = new List<InventoryItem>();

                // Add items to the looted items list, comparing a random number to the drop percentage
                foreach (LootItem lootItem in _currentMonster.LootTable)
                {
                    if (RandomNumberGenerator.NumberBetween(1, 100) <= lootItem.DropPercentage)
                    {
                        lootedItems.Add(new InventoryItem(lootItem.Details, 1));
                    }
                }

                // If no items were randomly selected, then add the default item(s)
                if (lootedItems.Count == 0)
                {
                    foreach (LootItem lootItem in _currentMonster.LootTable)
                    {
                        if (lootItem.IsDefaultItem)
                        {
                            lootedItems.Add(new InventoryItem(lootItem.Details, 1));
                        }
                    }
                }

                // Add the looted items to the player's inventory
                foreach (InventoryItem inventoryItem in lootedItems)
                {
                    _player.AddItemToInventory(inventoryItem.Details);

                    if (inventoryItem.Quantity == 1)
                    {
                        rtbMessages.Text += @"You loot " + inventoryItem.Quantity + " " + inventoryItem.Details.Name +
                                            Environment.NewLine;
                    }
                    else
                    {
                        rtbMessages.Text += @"You loot " + inventoryItem.Quantity + " " +
                                            inventoryItem.Details.NamePlural +
                                            Environment.NewLine;
                    }
                }

                // Refresh player information and inventory controls
                UpdatePlayerStats();
                UpdateInventoryListInUI();
                UpdateWeaponListInUI();
                UpdatePotionListInUI();

                // Add a blank line to the message box to make it look nice
                rtbMessages.Text += Environment.NewLine;

                // Move player to current location (to heal player and create a new monster fight)
                MoveTo(_player.CurrentLocation);
            }
            else
            {
                // Monster is still alive
                MonsterDamagetoPlayer();
            }
        }

        private void btnUsePotion_Click(object sender, EventArgs e)
        {
            // Get the currently selected potion from the combobox
            HealingPotion potion = (HealingPotion) cboPotions.SelectedItem;

            // Add healing amount to the player's current hit points
            _player.CurrentHitPoints = (_player.CurrentHitPoints + potion.AmountToHeal);

            // CurrentHitPoints cannot exceed MaxHitPoints
            if (_player.CurrentHitPoints > _player.MaximumHitPoints)
            {
                _player.CurrentHitPoints = _player.MaximumHitPoints;
            }

            // Remove the potion from the player's inventory
            foreach (InventoryItem ii in _player.Inventory)
            {
                if (ii.Details.ID == potion.ID)
                {
                    ii.Quantity--;
                    break;
                }
            }

            // Display message
            rtbMessages.Text += @"You drink a " + potion.Name + Environment.NewLine;

            // Monster gets their turn to attack
            MonsterDamagetoPlayer();

        }

        private void MonsterDamagetoPlayer()
        {
            // Determine the amount of damage the monster does to the player
            int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);

            // Display Message
            rtbMessages.Text += @"The " + _currentMonster.Name + @" hit you for " + damageToPlayer +
                                @" points of damage." + Environment.NewLine;

            // subtract damage from the player
            _player.CurrentHitPoints -= damageToPlayer;

            if (_player.CurrentHitPoints <= 0)
            {
                // Display message
                rtbMessages.Text += @"The " + _currentMonster.Name + @" killed you." + Environment.NewLine;

                // Move player to "home"
                MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
            }

            // Refresh player data in UI
            UpdatePlayerStats();
            UpdateInventoryListInUI();
            UpdatePotionListInUI();
        }

        private void DoesPlayerLevelUp()
        {
            if (_player.ExperiencePoints >= 50)
            {
                _player.Level += 1;
                _player.ExperiencePoints = 0;
                _player.MaximumHitPoints += 5;
                rtbMessages.Text += Environment.NewLine + @"You leveled up!" + Environment.NewLine;
                rtbMessages.Text += @"You gain 5 health." + Environment.NewLine +
                                    Environment.NewLine;
                lblExperience.Text = _player.ExperiencePoints.ToString();
                lblLevel.Text = _player.Level.ToString();
            }

        }

        private void UpdatePlayerStats()
        {
            lblHitPoints.Text = _player.CurrentHitPoints.ToString();
            lblGold.Text = _player.Gold.ToString();
            lblExperience.Text = _player.ExperiencePoints.ToString();
            lblLevel.Text = _player.Level.ToString();
        }

        private void rtbMessages_TextChanged(object sender, EventArgs e)
        {
            rtbMessages.SelectionStart = rtbMessages.Text.Length;
            rtbMessages.ScrollToCaret();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            const string savePath = @"C:\Users\Public\Documents\save.txt";
            string[] playerInformation =
            {
                _player.CurrentHitPoints.ToString(), _player.Gold.ToString(),
                _player.ExperiencePoints.ToString(), _player.Level.ToString()
            };

            File.WriteAllLines(savePath, playerInformation);

            MessageBox.Show("Game Saved Successfully.");
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            const string loadPath = @"C:\Users\Public\Documents\save.txt";
            try
            {
                string[] playerInformation = File.ReadAllLines(loadPath);
                _player.CurrentHitPoints = Convert.ToInt16(playerInformation[0]);
                _player.Gold = Convert.ToInt16(playerInformation[1]);
                _player.ExperiencePoints = Convert.ToInt16(playerInformation[2]);
                _player.Level = Convert.ToInt16(playerInformation[3]);
                UpdatePlayerStats();

                MessageBox.Show("Game Loaded Successfully.");
            }
            catch (FileNotFoundException)
            {
                
            }
        }

        
    }
}
