import * as React from 'react';

export default class Footer extends React.Component {
    componentWillUnmount() {
    }

    render() {
        return (
            
            <div className="container">
                
                <div className="row">&nbsp;</div>
                <footer>
                    <nav className="navbar navbar-expand-lg navbar-dark bg-dark">
                        <span>
                            Star Wars: Legends of the Old Republic is a free mod developed for Neverwinter Nights. Star Wars, the Expanded Universe, all logos, characters, artwork, stories, information, names, and other elements associated thereto, are sole and exclusive property of Lucasfilm Limited. We are a completely free, open source non-profit project and operate under the definition of "fair use" under United States copyright laws.
                        </span>
                    </nav>
                </footer>
            </div>
        );



    }
}
