import * as React from 'react';
import { Carousel } from 'react-responsive-carousel';
import SS1 from '../images/SWLOR_Image_1.jpg';
import SS2 from '../images/SWLOR_Image_2.jpg';
import SS3 from '../images/SWLOR_Image_3.png';
import SS4 from '../images/SWLOR_Image_4.png';
import SS5 from '../images/SWLOR_Image_5.jpg';
import SS6 from '../images/SWLOR_Image_6.png';

export default class Gallery extends React.Component {
    
    render() {
        return (
            <div>

                <div>
                    <div className="row">
                        <div className="col-12">
                            <div className="card border-primary mb-3 center">
                                <div className="card-body">
                                    <h4 className="card-title">Gallery</h4>

                                    <p>
                                        Check out some sweet in-game screenshots submitted by players!
                                     </p>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                </div>


                <Carousel>
                    <div>
                        <img src={SS4} alt="Screenshot 1" className="img-fluid" />
                    </div>
                    <div>
                        <img src={SS1} alt="Screenshot 2" className="img-fluid" />
                    </div>
                    <div>
                        <img src={SS5} alt="Screenshot 3" className="img-fluid" />
                    </div>
                    <div>
                        <img src={SS2} alt="Screenshot 4" className="img-fluid" />
                    </div>
                    <div>
                        <img src={SS6} alt="Screenshot 5" className="img-fluid" />
                    </div>
                    <div>
                        <img src={SS3} alt="Screenshot 6" className="img-fluid" />
                    </div>
                </Carousel>

            </div>
        );
    }
}
