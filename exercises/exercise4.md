---
layout: default
title: Exercise4
---

[Exercise 1]({{ site.baseurl }}{% link exercises/exercise1.md %}) | [Exercise 2]({{ site.baseurl }}{% link exercises/exercise2.md %}) | [Exercise 3]({{ site.baseurl }}{% link exercises/exercise3.md %}) | [Exercise 4]({{ site.baseurl }}{% link exercises/exercise4.md %}) | [Lecture Slides]({{ site.baseurl }}{% link ebd_lectureslides.md %}) | [Software]({{ site.baseurl }}{% link software.md %})

#  Analyzing behavioral differences across hospital typologies (submission: 29.04, in class)
Following the tutorial steps, compare the two hospital typologies you chose for the previous exercises for each of the following measures:

* **Circulation complexity: Inter-Connected Density (ICD)**, (O’Neill, 1991): ICD is the mean number of potential paths from any decision point within the floor plan. 
> Lower  ICD is correlated with better wayfinding 

* **Nursing routine efficiency: Yale Traffic Index (YTI)** (Thompson and Goldin, 1975)
The relative trip frequencies of selected traffic links multiplied by the distance between links. 
> Lower YTI is associated with more efficient nursing routines 

* **Quality of Care:  Spatial Communication Index (SCI)** (Pachilova and Sailer, 2020): SCI is the accumulated visibility for key traffic links in a ward divided by the number of beds. 
> Higher SCI is correlated with better quality of care 

**The analysis will be done using the three measures presented in class, and using the custom scripts built for the course (using grasshopper, see software page for instructions). Follow the steps in the video tutorial below to complete the analysis (please visit the software page and make sure you installed all the necessary software).**

**B. For each typology:**
* Export an image showing the analysis for each measure. 
* Drag and drop these files to the Miro board (link is sent in email with header _[EBD FS22] Exercise 4_) and compare the two typologies across the three measures. 
* Write a figure caption summarising your comparison. Your caption should be simple, and critical. What is the reason for the observed differences? Is it the configuration? The location of critical functions (e.g., the nurse station)? The size of the layout? 
![An example of the comparison matrix comparing both typologies across the three measures](/assets/images/ExampleMatrix.JPG)

**C. Submission files**
* Upload the images and the Rhino file (.3dm file, name as StudentFirstNameLastName.3dm) to polybox to this [link](https://polybox.ethz.ch/index.php/s/7x411XMgtLQvHk1)

[**D. DOWNLOAD LINK TOOLKIT AND EXAMPLE SCENE**](https://polybox.ethz.ch/index.php/s/1vsvOIQrjRRUXpv) (Password protected)
You will receive the password in the email with the header _[EBD FS22] Exercise 4_.
- **Example.3dm** is the example project containing the reference geometry.
- **metrics_new.gh** is the Grasshopper script you will use in Grasshopper.

**Update: Script Version of 21.04.2022**
There is a new version of the script and example project. With your feedback, we were able to further improve the tool.
Here is a summary of the changes:
- **Better Stability and Speed**: We have eliminated a few bugs that lead to faulty behaviour and improved execution speed.
- **Support for "holes" in Layout**: Previously, the graph was generated in the whole area that was enclosed by the curves of your layout. Now, you need to     delimit the areas that define boundaries (to the inside as well as the outside of the layout) as _closed curves_. To do so, break up the boundaries in straight segments and join them. The result should look something like this:

Outer Boundaries                             | Inner Boundaries
:-------------------------------------------:|:----------------------------------------:
![Outer](/assets/images/outer_boundary.JPG)  |  ![Inner](/assets/images/inner_boundaries.JPG)

    This will speed up calculation and give you more representative values for the ICD.
- **Configurable Graph Visualization**: We exposed some parameters to adapt the scale and position of the GraphBuilder visualization. This can be helpful if your graph is particularly large or small:
    <p float="left">
    <img src="/assets/images/graphbuilder_vis.JPG" width="100" />
    <img src="/assets/images/graphbuilder.png" width="100" /> 
    </p>
- **Configurable Position**: We exposed some parameters to adapt the placement of the numbers corresponding to each measure (SCI: XXX, YTI: YYY, ICD: ZZZ). To set it, right click > set point / set vector > and then choose the corresponding point / draw the vector on the Rhino screen. It will displace the measures from point "Measures Position" by "Measures Displacement.
    <p float="left">
    <img src="/assets/images/measure_positioning.JPG" width="100" />
    </p>


# Video Tutorial 
## 1. Preprocessing the Layout

<iframe width="560" height="315" src="https://www.youtube.com/embed/Z0VfHfC-WBo" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

* * *

## 2. Building the Graph

<iframe width="560" height="315" src="https://www.youtube.com/embed/-nIST0UFLoc" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

* * *

## 3. Visualizing Measures

<iframe width="560" height="315" src="https://www.youtube.com/embed/cvBViGg1eZY" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

* * *

## 4. Rendering a Visualization to an Image File

<iframe width="560" height="315" src="https://www.youtube.com/embed/5IcUh2u1Ox0" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

* * *
