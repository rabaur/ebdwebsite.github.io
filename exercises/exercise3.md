---
layout: default
title: Exercise3
permalink: /exercise3/
---

[Exercise 1]({{ site.baseurl }}{% link exercises/exercise1.md %})\
[Exercise 2]({{ site.baseurl }}{% link exercises/exercise2.md %})\
<!--[Exercise 3]({{ site.baseurl }}{% link exercises/exercise3.md %})\-->
[Lecture Slides]({{ site.baseurl }}{% link ebd_lectureslides.md %})\
[Software]({{ site.baseurl }}{% link software.md %})



# Simulating patients and caregivers journey in Virtual Reality




**In this exercise you will work in groups. There will be 2 groups in total.**
* **Group 1**: will simulate patients and caregivers journey in the existing Emergency Department (ED) at USZ.
* **Group 2**: will simulate patients and caregivers journey in the optimized Emergency Department (ED) at USZ. 
The goal is for you to quantitatively and qualitatively compare the differences between the two ED layouts with respect to patients and caregivers behavior and experience. 

Towards this end, you will model in 3D (using Visualarq for Rhino) the existing or optimized ED (based on the images provided in the Miro board, link sent via email). Using this model, you will perform the walkthrough in Unity3D (follow the tutorial below) from the perspective of three typical users (patient, nurse, visitor) 

**Submission date:** 5.11.2021

**Submission files:** 
* A zip folder containing the csv files for each path, a video recording (Screen recording) of the generated heat map (see example in the Miro board). 
* A panel (in Miro, a link will be sent via email)  showing the results for each group. 

**Submission Link (for zip folder):**  Drag and drop [here](https://polybox.ethz.ch/index.php/s/Unpf8bdLdv9IluV)
## Part 1: Model the ED layout 

## Part 1: Design the VR Walkthrough

* Describe the journey of 3 typical occupants **(e.g., nurse, patient, visitor)**
* **For each occupant type**, specify  how **familiar** is she/he with the building (novice/experienced), do they have a liability? are they stressed? are they tired?
* For each journey **describe** a **sequence of 3-4 typical activities** per occupant type. What is the **origin** and **destination** of each activity?
* For each walkthrough **provide** a **complementary diagram**. See an example for a walkthrough below.

![Example walkthrough design for a single occupant type](/assets/images/exercise3/image8.jpg)

*Example walkthrough design for a single occupant type*

![The affective appraisal questioner to capture participants ratings of architectural qualities.](/assets/images/exercise3/image5.jpg)

*The affective appraisal questioner to capture participants ratings of architectural qualities.*

![Google forms provides immediate analysis of your participants response.](/assets/images/exercise3/image4.jpg)

*Google forms provides immediate analysis of your participants response.*

![Table comparing a nurse’s experience across 3 typologies.](/assets/images/exercise/image1.jpg)

*Table comparing a nurse’s experience across 3 typologies.*

## Part 3: Setting up the experiment in Unity3D

We will use Unity3D in this exercise. In case you have not installed it yet, please do so following the instructions [here]({{ site.baseurl }}{% link software.md %}).

The UnityProject (to get accustomed with the setting) and tutorial videos:
* [UnityProject](https://polybox.ethz.ch/index.php/s/abt6xCtxFuwiPvj) of the tutorial (you can either build upon this project or make one from scratch).
* [Video 0](https://polybox.ethz.ch/index.php/s/WaQNPrHtU0ywizR): Reusing the tutorial project.
* [Video 1](https://polybox.ethz.ch/index.php/s/plabTFa6Bwi3yrc): Introduction
* [Video 2](https://polybox.ethz.ch/index.php/s/JNfsNKcsIIT0LHc): Setting up the Scene
* [Video 3](https://polybox.ethz.ch/index.php/s/IreHHFbtElgW1f6): Visual enhancements
* [Video 4](https://polybox.ethz.ch/index.php/s/6XkKS5MTcyIKBL5): Creating the Player
* [Video 5](https://polybox.ethz.ch/index.php/s/69hIifKjxULkMPV): Performing a VR-Walkthrough
* [Video 6](https://polybox.ethz.ch/index.php/s/nhrtDdD8AUxdQkq): Data Processing, Part 1
* [Video 7](https://polybox.ethz.ch/index.php/s/UGyfVgdqtO6VwpQ): Data Processing, Part 2

### Tutorial Overview

[Video 1](https://polybox.ethz.ch/index.php/s/plabTFa6Bwi3yrc)

This toolset will allow you make a more in-depth analysis of your virtual reality walkthroughs. More specifically, it allows you to:
Quantify how much screen-space is occupied by certain objects or sets of objects when performing the VR-experiment. You could use this information to answer questions like “After changing the layout, does an employee (in a work-place) see more apples or oranges when walking to his desk?”.

Visualize how much screen-space was occupied by certain objects during the VR-walkthrough. This will be achieved by overlaying a heatmap onto your scene, giving you direct visual cues where the VR-experiment-participant was looking at when wandering around.
In order to keep your scene organized, it is always a good idea to make use of the hierarchical structure Unity allows us to establish for GameObjects (the things that make up your virtual scene) and rename objects such that they have expressive names. We will follow this approach without specifically addressing this fact every time.
The steps are explained in short video sequences. This handout will complement these video clips by giving additional information about the scripts involved for each video. For each script, the meaning and suggested (or compulsory) domain of values of its parameters (the field you need to fill) are given. Example:

* * *
**Script Name (Script)**
* _Parameter Name 1_ (The accepted values you can choose): What this parameter controls in the script and how you should go about setting this parameter.
* _Parameter Name 2_ (The accepted values you can choose): What this parameter controls in the script and how you should go about setting this parameter.
* …

* * *

### Setting up the Scene

If you have already received a scene which is fully set up, you can skip this section. If you need to change up your analysis or start with a custom (or empty) scene, you should probably stick around. In general, our setup consists of the following stages:
* Import your building into Unity and making sure the import-parameters are correctly set.
* Create an FPS-like (we are borrowing some gaming-jargon here: first person shooter) player to perform the VR-walkthrough.
* Assigning different layers to the objects constituting our scene corresponding the different types of objects we want to discern.
* Creating an object as a parent for the script we will use to process the data from the VR-walkthrough.

[Video 2](https://polybox.ethz.ch/index.php/s/JNfsNKcsIIT0LHc)

## Making it Pretty (Optional)

Currently, Unity is using its basic shader model to light your scene. For the full visual glory, follow the steps of this part of the tutorial.

[Video 3](https://polybox.ethz.ch/index.php/s/IreHHFbtElgW1f6)

## Collecting the Data

You can either **create** the Player or **re-use** the one from the tutorial (which is a lot less work). To find out how to re-use the Player, make sure to watch [Video 0](https://polybox.ethz.ch/index.php/s/WaQNPrHtU0ywizR).

First, we are going to create the character which moves around the scene when performing the VR-experiment. We will refer to this character as the **Player**.

The Player consists of:
* A Capsule named **Player**. It makes up the virtual body of our character. While one could attach a more complex model, this simple placeholder will suffice at this stage.
* A camera named **FOV**. We attach a “Mouse Tracker” script that will track our mouse-movement when in GameMode. Through this camera one will perceive the scene when performing the VR-experiment. To avoid unexpected behaviour either use the existing “Main Camera” in your scene or create a new camera object and make sure no other camera is in the scene by deleting any other camera in the hierarchy.
* An empty GameObject named **GroundCheck**. It is the center of a collision-check the Player performs to determine if it is on the ground or not. Place this at the lowest point of Body.

[Video 4](https://polybox.ethz.ch/index.php/s/6XkKS5MTcyIKBL5)

* * *
**PlayerMovement (Script)**
* **Controller** (Character Controller): References the script that will interpret the input we are capturing in the Player Movement script. Drag and drop the Character Controller you have previously attached to this GameObject.
* **Movement Speed** (Positive Number): Defines how fast your character will move around the scene.
* **Gravity** (Negative Number): Controls the force of the gravitational force the character will be exposed to.
* **Ground Check** (GameObject): Reference to the GameObject which is used whether the character is on the ground or not. The referenced object should be located at the lowest point of Body to yield sensible results.
* **Check Radius** (Positive Number): The radius of the sphere around the center of Ground Check in which the script will check for collision with the ground. Choose this number to be reasonably small (or leave it at the default value), otherwise your character will float above or fall through the ground.
* **Layer Mask** (Choice): Choose the layers you want to act as ground for the character.
* **Jump Height** (Positive number): How high you want the Player to jump.

* * *

* * *
**MouseTracker** (Script)
* **Mouse Sensitivity** (Positive Number): How sensitive in-game movement is to the physical translation of your mouse.
* **Player Body** (GameObject): Reference to the complete Player, not just the Body GameObject. Insert this to make you character respond to mouse-movement.
* **Max Dorsal** (Positive Number, degrees): How far back can the Player tilt its head from neutral position. Default is quite realistic for humans.
* **Max Ventral** (Positive Number, degrees): How far forward can the Player tilt its head from neutral position. Default is quite realistic for humans.

* * *
### Layers
Every GameObject in your scene is assigned to a specific layer. By default this is the layer “Default”. In order to produce meaningful results, you need to assign the GameObjects of interest to different layers of your choice. Follow the instructions in the video to find out how.

To gather data, we attach the Capture Walkthrough script to the Player. All files generated will be in the UnityProject-folder (the script allows you to specify the location more precisely). All the script really does is to record the position of the Player and the direction it is facing in three-dimensional and writes this data to a .csv file. This file can later be used to reconstruct the trajectory the Player has wandered during the VR-experiment.

A VR-walkthrough can simply be conducted by hitting the “Play”-Button at the top of the editor. Make sure to activate all previous scripts such that the Player will move around. End the experiment by clicking “Play”-Button again.

[Video 5](https://polybox.ethz.ch/index.php/s/69hIifKjxULkMPV)

* * *
**CaptureWalkthrough** (Script)
* **Sample Interval** (Positive Number): The time that must pass until a new sample consisting of the current location of the Player and the direction it is facing is recorded. Should you experience any performance issues during the VR-walkthrough, try increasing this number (thus decreasing the frequency at which samples are generated).
* **Dir Name** (Text): Name of the directory you want to save your data-files to. This directory will be in the UnityProject-folder. If the directory does not exist, it will be automatically generated.
* **File Name** (Text): Base name of your file. If Force Write is deactivated, the script will attach a unique identifier to this name when there is an existing file with the same name. This avoids overwriting data by accident.
* **Format** (Text): Extension of the generated file. Default is .csv, there should be no necessity to change.
* **Force Write** (True/False): Activate to overwrite existing files with the same name.
* **View** (GameObject): Reference to the camera which perceives the VR-walkthrough (View).

* * *

### Processing and Analysing the Data
At this point you should have been able to generate some data. Now we will crunch the numbers!

Create a new empty GameObject and attach the ProcessWalkthrough component. This script will take the previously captured data and reconstruct the trajectory of the Player during the VR-walkthrough. At each sample-position, the script performs a ray-cast (sending a lot of rays from a position in space). To model the field of view of a human, the rays are cast within a cone-like shape, where the tip of the cone is the position of View at that sample and the axis of the cone is the direction View was facing at that sample. By adjusting the values of the ProcessWalkthrough script, you can modify the shape of the cone for your needs. The rays themselves are randomly distributed within this cone, all originating from the tip of the cone.

Whenever the rays are cast into the scene, the script records with which GameObjects they collide (if any). Here the previously definied layers come into play: The script can detect which layer the objects belong that are hit. The number of hits are aggregated per layer and normalized such that they represent the degree of visual attention (in percentages). This allows you to make rigorous numerical statement about the visual attention pattern of you VR-walkthrough participants.

The second evaluation visualizes the number of hits are a specific object received during the walkthrough, corresponding to how salient this object is in the scene. In order to make this analysis more fine-grained, hits are not aggregated per GameObject as before, but displayed as small, easy to render GameObjects, called Particles. Therefore we can directly visually perceive the visual attention patterns of our participants. The color of the particles can be set by a modifiable gradient.

Note that the computational efforts to process many particles is rather high. We suggest that you increase the parameter “Rays per Raycast” step by step, starting off with a low value like 5. Once you have generated a satisfactory result, you can reuse the distribution of particles and colors by ticking “Reuse Data” and giving the path to the file you have previously captured containing the locations of the particles and their colors.

[Video 6](https://polybox.ethz.ch/index.php/s/nhrtDdD8AUxdQkq)
[Video 7](https://polybox.ethz.ch/index.php/s/UGyfVgdqtO6VwpQ)

* * *
**ProcessWalkthrough** (Script)
* **Layer Mask** (Choice): Tick all layers that you want to analyse. Only then the script will take the objects belonging to this layer into consideration. This allows you to easily add or remove layers.
* **Gradient** (Customizable Color Gradient): Here you can define the color gradient that is used to visualize the intensity of visual attention parts of you scene have received. 0 corresponds to the highest degree and 1 to the lowest.
* **Reuse Data** (True/False): Check this box if you have already generated a file which contains the position of particles and their respective colors. This will prevent you from having to recompute another distribution of particles from scratch and the script will only be busy visualizing the particles. This will save you substantial time when tweaking the optical features of your visualisation (as the color of the gradient or the size of the particles).
* **In Path Walkthrough** (Text): Path to the file that contains the spatial distribution of particles and their colors from a previous execution of ProcessWalkthrough. This file will be read when “Reuse Data” is active.
* **Out Dir Heatmap** (Text): Directory where the file containing the spatial distribution of the particles and their colors will be saved to.
* **Out File Name Heatmap** (Text): Filename of the file containing the spatial distribution of the particles and their colors will be saved to. Will generate file with modified name if file with same name already exists.
* **Out Dir Statistic** (Text): Directory where the file containing the percentages of visual attention per layer is saved to.
* **Out File Name Statistic** (Text): Filename of the file containing the percentages of visual attention per layer. Will generate file with modified name if file with same name already exists.
* **Horizontal View Angle** (Number, positive and smaller than 180): The angle in degrees that the cone spans along the horizontal plane.
* **Vertical View Angle** (Number, positive and smaller than 180): The angle in degrees that the cone spans along the vertical plane.
* **Rays Per Raycast** (Number, positive): Start low, increase according to the processing power of you system.
* **Particle Size** (Number, positive): Size of the particles that are used to visualize the visual attention

* * *

### Visualize Trajectories
This script will visualize the VR-walkthroughs conducted in Exc#5. You need to input the walkthrough file of interest, and upon running the GameMode you will:
* See the trajectory described by the walkthrough in the scene (with customizable color and width).
* See the distance that the agent has covered during the walkthrough.
The distance is indicated in red. Please download this new script [here](https://polybox.ethz.ch/index.php/s/hBGphEwWugH59cT).

