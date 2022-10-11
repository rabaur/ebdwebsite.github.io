---
layout: default
title: Exercise2
permalink: /exercise2/
---

[Software]({{ site.baseurl }}{% link software.md %}) | [Exercise 1]({{ site.baseurl }}{% link exercises/exercise1.md %}) | [Exercise 2]({{ site.baseurl }}{% link exercises/exercise2.md %})

# Exc#2: Agent-based simulations to analyse design tradeoffs

## Overview:
- You will generate 3D models of Emergency Departments (EDs) you would like to compare with respect to the dynamics of care delivery 
- You will run an agent-based simulation of the same activities across selected ED layouts
- You will analyse the results generated and assess the design tradeoffs across ED layouts
- This is a team-exercise (in pairs or up to 4 team members max). Depending on the size of your team you will need to compare less or more layouts. The idea is that each student will model one ED, such that a team of 2 will compare 2 EDs, and a team of 4 will compare 4 EDs.
- The submission includes a presentation in class on the 12.10.2022, 10 minutes per team.

## Objectives:
- The aim of this exercise is to apply agent-based simulation to understand how different ED layouts accommodate the same sequence of activities. For instance, in an ED, different occupant groups (e.g., visitors, nurses, physicians, patients)  perform different activities at the same time (e.g., walking, lying in bed, waiting, distributing medicine) . This exercise will help you measure how the **same activities** unfold across **different** Emergency Department layouts and realise design tradeoffs resulting from the fit between the layout and the activities it is meant to accomodate.These tradeoffs are reflected across differences in the following measures:
    - Average walking distances (could reflect effort, and fatigue) per occupant groups (e.g., all nurses) or individual occupants/agents (e.g., one nurse). 
    - Average activity duration per occupant group or individual occupant/agent
    - Interactions between agents/collisions (can reflect potential for social interactions or interruptions)

**Submission date:** 12.10.2022, 16:00

**Presentation:** 12.10.2022, in class. Each team will get up to 10 minutes to present the results of their exercise

**Submission files:**
- A single Rhino file containing all ED models you compared (make sure that the scale of the models is correct). 
- A zip folder that includes your Unity project (which should include the scenes you used, the fbx models and the data files from the simulation).
- A presentation (10 slides max) covering your design hypotheses, simulation runs, results and interpretation of design tradeoffs.

**Submission Link (for zip folder):** Drag and drop [here](https://polybox.ethz.ch/index.php/s/W8ZEiauxvYnv9IT) 
What will you need to do?

## The exercise stages are:
- Choose and generate models of ED layouts you would like to compare. Make sure you understand the layouts in terms of scale, positioning of functions, is this a big-city ED or is it a small-town one? Where is the entrance, exist, nurse stations, patient rooms, coffee machine, medicine rooms, offices, etc? You need to understand how the layouts differ in these respects to make sense of the results. 
- To model the layouts we will use VisualArq for Rhino. If you prefer to use another modelling software, you can do so. However, note that you will need to use Rhino in future exercises, so some early experience might be beneficial. A tutorial showing how to model a floor plan from an image using visualarq can be found [here](https://drive.google.com/file/d/1NVp7wVHzlMYqUnkfs7qSa9iyu5t3iO9D/view). For additional VisualARQ tutorials, please visit [this link](https://www.visualarq.com/learn/videos/)
- Export both typologies from Rhino as .fbx files (or another format that is accepted by Unity3D) by following these steps:
<iframe width="560" height="315" src="https://www.youtube.com/embed/XsIkjZUcI-U" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

- Use the activity schedule provided [in class](https://docs.google.com/spreadsheets/d/1eSNupJELLTg8enOfSLfexkW3Xz9i4ACvlN7clGLABbo/edit?usp=sharing) to program your simulation according to the tutorial. Please make sure you 
- Before you run the simulation of the activity sequence outlined in the table (for running the simulation please follow the tutorial below), hypothesise what would be the differences between the two ED layouts. For instance:
>Since the racetrack typology has the nurse station at the centre, I expect that the average walking distance of nurses to be lower when compared to a courtyard typology in which the nurse station is situated at the corners of the layout, requiring more walking towards patient rooms.

    **Note: In your report you should have one hypothesis per measure (e.g., for social interactions/interruptions, walking distances, density).**
- Using the data collected: compare average scores across metrics for relevant agents across EDs (see exemplar table for one typology in the lecture slides). Compare activity heatmaps across EDs. Assuming the same sequence of activities, what are the differences in the spatial distribution of activities in each typology?
- Interpret the results, which ED layout performed better? Can you identify design trade-offs?

## Unity tutorial: Running the simulation in Unity3D
The UnityProject (to get accustomed with the setting) and tutorial videos:
- [UnityProject](https://polybox.ethz.ch/index.php/s/xT5jVl4cjD3IidW) of the tutorial (you can either build upon this project or make one from scratch).
- [Video 1](https://polybox.ethz.ch/index.php/s/uMOz8s2afNEMUDO): Import and setup of the scene
- [Video 2](https://polybox.ethz.ch/index.php/s/dI1hflUU9iJ4j02): Setting up points of interest
- [Video 3](https://polybox.ethz.ch/index.php/s/7I2T8U6SWxjZmEm): Defining task

    The provided video shows an older version of _EngineScript.cs_. The new version also requires you to set the **Data Folder**=_your_data_folder_name_ and **File Name**=_your_file_name_ parameters. The **Data Folder** indicates the folder _within_ your Unity project folder where the experimental data is saved to. Once a simulation is run, your data will be saved in this folder. It is a the top level of your Unity Project directory, which you can find via the Unity Hub.

    <figure>
        <img src="/assets/images/exercise2/where.png" style="max-width: 500px;"
            alt="The location of the Unity folder" />
        <figcaption>The location of the Unity folder</figcaption>
    </figure>

    <figure>
        <img src="/assets/images/exercise2/unity_folder.png" style="max-height: 300px;"
            alt="Jekyll logo" />
        <figcaption>Where the data is saved to</figcaption>
    </figure>

    <figure>
        <img src="/assets/images/exercise2/engine_config.png" style="max-width: 500px;"
            alt="Jekyll logo" />
        <figcaption>An example of how to set up the new EngineScript</figcaption>
    </figure>
- [Video 4](https://polybox.ethz.ch/index.php/s/ubLAQQ0NZtmjwKy): Visualization

    Update: Previous video showed an outdated version of the script. Created new video.

- [Video 5](https://polybox.ethz.ch/index.php/s/kHqzedOnTPVJEz7): Recording Footage
