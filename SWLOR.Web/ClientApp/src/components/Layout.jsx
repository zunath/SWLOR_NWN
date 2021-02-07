import * as React from 'react';
import Header from './Header';
import Footer from './Footer';

export class Layout extends React.Component {


    render() {
        return <div>
            <div className="container">
                <Header />
            </div>


            <div className="row">&nbsp;</div>

            <div className="container">

                {this.props.children}

            </div>
            
           <div className="row">&nbsp;</div>

            <Footer />

        </div>;
    }
}
